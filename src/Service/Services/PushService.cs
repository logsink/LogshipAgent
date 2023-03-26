using Logship.Agent.Service.Collectors;
using Logship.Agent.Service.Configuration;
using Logship.Agent.Service.Records;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Logship.Agent.Service.Services
{
    internal sealed class PushService : BaseAsyncService
    {
        private readonly PushConfiguration pushConfiguration;
        private readonly ILogger logger;
        private readonly InfoSink infoSink;
        private readonly HttpClient client;

        public PushService(PushConfiguration pushConfiguration, InfoSink infoSink, ILogger logger)
            : base("PushService", logger)
        {
            this.logger = logger;
            this.pushConfiguration = pushConfiguration;
            this.infoSink = infoSink;
            this.client = new HttpClient();
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (false == token.IsCancellationRequested)
            {

                // Delay for the configured push interval
                await Task.Delay(this.pushConfiguration.Interval, token);

                // Get the next batch of records from the info sink
                using (var records = this.infoSink.GetNextBatch())
                {
                    if (records.Length == 0)
                    {
                        continue;
                    }

                    using var memoryStream = new MemoryStream();
                    using (var writer = new Utf8JsonWriter(memoryStream))
                    {
                        JsonSerializer.Serialize(writer, records.Records, RecordSourceGenerationContext.Default.IEnumerableDataRecord);
                    }

                    memoryStream.Position = 0;

                    var request = new HttpRequestMessage(HttpMethod.Put, $"{this.pushConfiguration.Endpoint}/inflow/{Guid.Empty}");
                    request.Content = new StreamContent(memoryStream);

                    try
                    {
                        await this.client.SendAsync(request, token);
                        records.Success();
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError("Failed to push metrics. {error}", ex.Message);
                    }

                    this.logger.LogDebug("Successfully push {count} metrics", records.Length);
                }
            }
        }

        protected override Task OnStop(CancellationToken token)
        {
            this.client.Dispose();
            return base.OnStop(token);
        }
    }
}
