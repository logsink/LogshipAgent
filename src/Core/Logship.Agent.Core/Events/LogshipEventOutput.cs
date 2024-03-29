using Logship.Agent.Core.Configuration;
using Logship.Agent.Core.Records;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace Logship.Agent.Core.Events
{
    internal sealed class LogshipEventOutput : IEventOutput, IDisposable
    {
        private readonly string endpoint;
        private readonly Guid subscription;
        private readonly ILogger<LogshipEventOutput> logger;
        private readonly HttpClient client;

        public LogshipEventOutput(IOptions<OutputConfiguration> config, IHttpClientFactory httpClientFactory, ILogger<LogshipEventOutput> logger)
        {
            this.endpoint = config.Value.Endpoint;
            this.subscription = config.Value.Subscription;
            this.logger = logger;
            this.client = httpClientFactory.CreateClient();
            EventsLog.Endpoint(logger, this.endpoint, this.subscription);
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
                EventOutputLog.PushingMetics(this.logger);
                var response = await this.client.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    EventOutputLog.PushingMeticsSuccessful(this.logger);
                }
                else
                {
                    EventOutputLog.PushingMeticsFailed(this.logger, response.RequestMessage!.RequestUri, response.StatusCode, response.Content.ToString() ?? string.Empty);
                    return false;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { /* noop */ }
            catch (Exception ex)
            {
                EventOutputLog.PushingMetricsException(this.logger, ex);
                throw;
            }

            return true;
        }
    }

    internal static partial class EventOutputLog
    {
        [LoggerMessage(LogLevel.Debug, "Pushing metrics...")]
        public static partial void PushingMetics(ILogger logger);

        [LoggerMessage(LogLevel.Debug, "Successfully pushed metrics.")]
        public static partial void PushingMeticsSuccessful(ILogger logger);

        [LoggerMessage(LogLevel.Error, "Failed to push HTTP metrics to {RequestUrl}. {Status} {Message}")]
        public static partial void PushingMeticsFailed(ILogger logger, Uri? requestUrl, HttpStatusCode status, string message);

        [LoggerMessage(LogLevel.Error, "Failed to push metrics.")]
        public static partial void PushingMetricsException(ILogger logger, Exception exception);

    }
}
