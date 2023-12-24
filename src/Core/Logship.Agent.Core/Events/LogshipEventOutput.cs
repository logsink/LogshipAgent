using Logship.Agent.Core.Configuration;
using Logship.Agent.Core.Records;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Logship.Agent.Core.Events
{
    public class LogshipEventOutput : IEventOutput, IDisposable
    {
        private readonly string endpoint;
        private readonly Guid subscription;
        private readonly ILogger logger;
        private readonly HttpClient client;

        public LogshipEventOutput(string endpoint, Guid subscription, ILogger logger)
        {
            this.endpoint = endpoint;
            this.subscription = subscription;
            this.logger = logger;
            this.client = client = new HttpClient();
        }

        public void Dispose()
        {
            ((IDisposable)client).Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<bool> SendAsync(IReadOnlyCollection<DataRecord> records, CancellationToken cancellationToken)
        {
            if (records.Count == 0)
            {
                return true;
            }

            using var memoryStream = new MemoryStream(capacity: records.Count * 100);
            using (var writer = new Utf8JsonWriter(memoryStream))
            {
                await JsonSerializer.SerializeAsync(memoryStream, records, RecordSourceGenerationContext.Default.IReadOnlyCollectionDataRecord, cancellationToken);
            }

            memoryStream.Position = 0;
            var request = new HttpRequestMessage(HttpMethod.Put, $"{this.endpoint}/inflow/{this.subscription}")
            {
                Content = new StreamContent(memoryStream)
            };

            using var sendScope = this.logger.BeginScope(new Dictionary<string, object>
            {
                ["Records"] = records.Count,
                ["Endpoint"] = this.endpoint,
            });

            try
            {
                this.logger.LogDebug("Pushing metrics...");
                var response = await this.client.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    this.logger.LogDebug("Successfully pushed metrics.");
                }
                else
                {
                    this.logger.LogError("Failed to push HTTP metrics to {requestUrl}. {status} {message}", response.RequestMessage!.RequestUri, response.StatusCode, response.Content.ToString());
                    return false;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { /* noop */ }
            catch (Exception ex)
            {
                this.logger.LogError("Failed to push metrics. {error}", ex.Message);
                throw;
            }

            return true;
        }
    }
}
