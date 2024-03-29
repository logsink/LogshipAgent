using Logship.Agent.Core.Configuration;
using Logship.Agent.Core.Events;
using Logship.Agent.Core.Inputs.Common.Udp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Logship.Agent.Core.Services.Sources.Common.Udp
{
    internal sealed class UdpListenerService : BaseInputService<UDPListenerConfiguration>, IDisposable
    {
        private UdpClient? udpClient;

        protected override bool ExitOnException => false;

        public UdpListenerService(IOptions<SourcesConfiguration> config, IEventBuffer buffer, ILogger<UdpListenerService> logger)
            : base(config.Value.UDPListener, buffer, nameof(UdpListenerService), logger)
        {
        }

        public void Dispose()
        {
            udpClient?.Dispose();
            udpClient = null;
        }

        protected override Task OnStart(CancellationToken token)
        {
            var remoteEndpoint = new IPEndPoint(IPAddress.Any, this.Config.Port);
            this.udpClient = new UdpClient(remoteEndpoint);
            return base.OnStart(token);
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (false == token.IsCancellationRequested)
            {
                try
                {
                    var data = await udpClient!.ReceiveAsync(token);
                    await AddUdpMessageAsync(data.Buffer.ToArray(), token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { }
                catch (Exception ex)
                {
                    UdpListenerLog.Error(Logger, ex);
                }
            }
        }

        private async Task AddUdpMessageAsync(byte[] message, CancellationToken token)
        {
            if (message.Length == 0)
            {
                return;
            }

            using var stream = new MemoryStream(message);
            stream.Position = 0;
            var data = await JsonSerializer.DeserializeAsync(stream, UdpMessageSerializationContext.Default.UdpMessage, token);
            if (data != null
                && data.Data != null
                && data.Data.Count > 0
                && false == string.IsNullOrWhiteSpace(data.Schema))
            {
                var record = CreateRecord(data.Schema, data.Timestamp);
                foreach (var kvp in data.Data)
                {
                    if (kvp.Value is JsonElement element)
                    {
                        switch (element.ValueKind)
                        {
                            case JsonValueKind.Object:
                            case JsonValueKind.Array:
                                {
                                    using var textStream = new MemoryStream();
                                    var writer = new Utf8JsonWriter(textStream);
                                    element.WriteTo(writer);
                                    writer.Flush();
                                    record.Data[kvp.Key] = Encoding.UTF8.GetString(textStream.ToArray());
                                }
                                break;
                            case JsonValueKind.String:
                                record.Data[kvp.Key] = element.GetString() ?? string.Empty;
                                break;
                            case JsonValueKind.Number:
                                record.Data[kvp.Key] = element.GetDecimal();
                                break;
                            case JsonValueKind.True:
                                record.Data[kvp.Key] = true;
                                break;
                            case JsonValueKind.False:
                                record.Data[kvp.Key] = false;
                                break;
                            case JsonValueKind.Null:
                            case JsonValueKind.Undefined:
                                continue;
                        }
                    }
                    else
                    {
                        record.Data[kvp.Key] = kvp.Value;
                    }
                }

                Buffer.Add(record);
            }
        }
    }

    internal static partial class UdpListenerLog
    {
        [LoggerMessage(LogLevel.Error, "UDP Client exception.")]
        public static partial void Error(ILogger log, Exception ex);
    }
}
