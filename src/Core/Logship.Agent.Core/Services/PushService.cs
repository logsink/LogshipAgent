using Logship.Agent.Core.Configuration;
using Logship.Agent.Core.Events;
using Logship.Agent.Core.Records;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Logship.Agent.Core.Services
{
    internal sealed class PushService : BaseConfiguredService
    {
        private TimeSpan interval;
        private readonly IEventSink eventSink;

        public PushService(IEventSink eventSink, ILogger logger)
            : base(nameof(PushService), logger)
        {
            this.eventSink = eventSink;
        }

        public override void UpdateConfiguration(IConfigurationSection configuration)
        {
            this.interval = configuration.GetTimeSpanValue(nameof(interval), TimeSpan.FromSeconds(30), this.Logger);
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (false == token.IsCancellationRequested)
            {

                // Delay for the configured push interval
                await Task.Delay(this.interval, token);
                try
                {
                    this.Logger.LogDebug("Starting event sink flush");
                    await this.eventSink.FlushAsync(token);
                    this.Logger.LogDebug("Successfully flushed metrics");
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { /* noop */ }
                catch (Exception ex)
                {
                    this.Logger.LogError("Failed to flush event sink. {exception}", ex);
                }
            }
        }
    }
}
