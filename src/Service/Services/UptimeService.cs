using Logship.Agent.Service.Configuration;
using Microsoft.Extensions.Logging;

namespace Logship.Agent.Service.Collectors
{
    /// <summary>
    /// This class just post a 
    /// </summary>
    internal class UptimeService : BaseAsyncService
    {
        private readonly UptimeServiceConfiguration configuration;
        private readonly DateTimeOffset startupTime;
        private readonly InfoSink sink;

        public UptimeService(UptimeServiceConfiguration configuration, InfoSink sink, ILogger logger)
            : base("UptimeService", logger)
        { 
            this.configuration = configuration;
            this.sink = sink;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (false == token.IsCancellationRequested)
            {
                this.sink.Push(new Records.DataRecord("Logship.Agent.Uptime", DateTimeOffset.UtcNow, new Dictionary<string, object>
            {
                {  "startTime", this.startupTime.ToString("O") },
                {  "value", (DateTimeOffset.UtcNow - this.startupTime).ToString("c") }
            }
            ));
                await Task.Delay(this.configuration.Interval, token).ConfigureAwait(false);
            }
            
        }
    }
}
