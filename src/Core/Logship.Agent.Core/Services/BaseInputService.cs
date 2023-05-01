using Logship.Agent.Core.Events;
using Logship.Agent.Core.Records;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logship.Agent.Core.Services
{
    public abstract class BaseInputService : BaseConfiguredService
    {
        protected readonly IEventBuffer Buffer;
        protected TimeSpan Interval;

        protected virtual bool ExitOnException { get; set; } = true;

        public BaseInputService(IEventBuffer buffer, string serviceName, ILogger logger) : base(serviceName, logger)
        {
            this.Buffer = buffer;
        }

        public override void UpdateConfiguration(IConfigurationSection configuration)
        {
            this.Interval = configuration.GetTimeSpan(nameof(Interval), Interval, this.Logger);
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (false == token.IsCancellationRequested)
            {
                try
                {
                    await this.ExecuteSingleAsync(token);
                    await Task.Delay(this.Interval, token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { /* noop */ }
                catch (Exception ex)
                {
                    this.Logger.LogError("Exception during execute service {serviceName}. ExitOnException = {exitOnException}. {exception}", this.serviceName, this.ExitOnException, ex);
                    if (this.ExitOnException)
                    {
                        break;
                    }
                }
            }
        }

        protected abstract Task ExecuteSingleAsync(CancellationToken token);

        protected static DataRecord CreateRecord(string schema, DateTimeOffset? timestamp = null)
        {
            timestamp ??= DateTimeOffset.UtcNow;
            var data = new Dictionary<string, object>()
            {
                { "machine", Environment.MachineName },
            };
            return new DataRecord(schema, timestamp.Value, data);
        }
    }
}
