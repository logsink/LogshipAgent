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
            this.interval = configuration.GetTimeSpan(nameof(interval), TimeSpan.FromSeconds(30), this.Logger);
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (false == token.IsCancellationRequested)
            {
                try
                {
                    // Delay for the configured push interval
                    await Task.Delay(this.interval, token);
                    this.Logger.LogTrace("Starting event sink flush");
                    await this.eventSink.FlushAsync(token);
                    this.Logger.LogTrace("Successfully flushed metrics");
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    this.Logger.LogError("Failed to flush event sink. {exception}", ex);
                }
            }
        }
    }
}
