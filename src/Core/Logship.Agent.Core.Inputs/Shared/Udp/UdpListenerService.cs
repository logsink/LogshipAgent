using Logship.Agent.Core.Events;
using Logship.Agent.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Logship.Agent.Core.Inputs.Shared.Udp
{
    internal class UdpListenerService : BaseInputService, IDisposable
    {
        private UdpClient? udpClient;
        private int port;

        public UdpListenerService(IEventBuffer buffer, ILogger logger) : base(buffer, nameof(UdpListenerService), logger)
        {
        }

        protected override TimeSpan DefaultInterval => TimeSpan.FromMilliseconds(1);

        public override void UpdateConfiguration(IConfigurationSection configuration)
        {
            base.UpdateConfiguration(configuration);
            int port = configuration.GetInt(nameof(port), 59999, Logger);
            if (udpClient == null)
            {
                this.udpClient = new UdpClient(port);
                this.port = port;
            }
            else if (this.port != port)
            {
                this.Logger.LogError("Attempted to change UDP listener port. Old = {oldPort}. New = {newPort}", this.port, port);
            }
        }

        public void Dispose()
        {
            this.udpClient?.Dispose();
            this.udpClient = null;
        }

        protected override async Task ExecuteSingleAsync(CancellationToken token)
        {
            if (udpClient != null)
            {
                try
                {
                    // var remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
                    var data = await udpClient.ReceiveAsync(token);
                    await this.AddUdpMessageAsync(data.Buffer.ToArray(), token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { }
                catch (Exception ex)
                {
                    this.Logger.LogError("UDP Client exception: {exception}", ex);
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
                foreach(var kvp in data.Data)
                {
                    record.Data[kvp.Key] = kvp.Value.ToString() ?? string.Empty;
                }

                this.Buffer.Add(record);
            }
        }
    }
}
