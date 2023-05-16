using Logship.Agent.Core.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Logship.Agent.Core.Services
{
    /// <summary>
    /// This class just post a 
    /// </summary>
    internal class HealthService : BaseConfiguredService
    {
        public static string ConfigTypeName => nameof(HealthService);

        private readonly DateTimeOffset startupTime;
        private readonly IEventBuffer sink;
        private TimeSpan interval;

        public HealthService(IEventBuffer sink, ILogger logger)
            : base(nameof(HealthService), logger)
        { 
            this.sink = sink;
            this.startupTime = DateTimeOffset.UtcNow;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (false == token.IsCancellationRequested)
            {
                this.sink.Add(new Records.DataRecord("Logship.Agent.Uptime", DateTimeOffset.UtcNow, new Dictionary<string, object>
                {
                    { "machine", Environment.MachineName },
                    { "startTime", this.startupTime.ToString("O") },
                    { "interval", this.interval.ToString("c") },
                    { "value", (DateTimeOffset.UtcNow - this.startupTime).ToString("c") },
                    { "counter", (DateTimeOffset.UtcNow - this.startupTime).TotalMilliseconds }
                }));

                await Task.Delay(this.interval, token).ConfigureAwait(false);
            }
        }

        public override void UpdateConfiguration(IConfigurationSection configuration)
        {
            this.interval = configuration.GetTimeSpan(nameof(this.interval), TimeSpan.FromSeconds(15), this.Logger);
        }
    }
}
