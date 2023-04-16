using Logship.Agent.Core.Events;
using Logship.Agent.Core.Services;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Logship.Agent.Core.Inputs.Windows.Etw
{
    public class EtwService : BaseConfiguredService, IDisposable
    {
        private static readonly string SessionNamePrefix = $"Logship-Agent-{nameof(EtwService)}";

        private string sessionNamePrefix = SessionNamePrefix;
        private bool cleanupOldSessions;
        private bool reuseExistingSession;

        private ITraceEventSession? traceEventSession;
        private readonly IEventBuffer eventBuffer;

        private IReadOnlyList<ProviderConfiguration> providers = Array.Empty<ProviderConfiguration>();
        public Func<ITraceEventSession> SessionFactory { get; set; }

        private bool disposedValue;

        public EtwService(IEventBuffer eventBuffer, ILogger logger) : base(nameof(EtwService), logger)
        {
            this.disposedValue = false;
            this.eventBuffer = eventBuffer;
            this.SessionFactory = () => new StandardTraceEventSession(this.sessionNamePrefix, this.cleanupOldSessions, this.reuseExistingSession);
        }

        public override void UpdateConfiguration(IConfigurationSection configuration)
        {
            this.sessionNamePrefix = configuration.GetValueOrDefault(nameof(sessionNamePrefix), str => str ?? SessionNamePrefix, this.Logger) ?? SessionNamePrefix;
            this.cleanupOldSessions = configuration.GetValueOrDefault(nameof(cleanupOldSessions), str => bool.TryParse(str, out var result) ? result : true, this.Logger);
            this.reuseExistingSession = configuration.GetValueOrDefault(nameof(reuseExistingSession), str => bool.TryParse(str, out var result) ? result : true, this.Logger);

            var providerSection = configuration.GetSection("providers");
            var newProviders = new List<ProviderConfiguration>();
            foreach (var provider in providerSection.GetChildren())
            {
                if (ProviderConfiguration.TryFrom(provider, this.Logger, out var providerConfiguration))
                {
                    newProviders.Add(providerConfiguration);
                }
            }

            if (traceEventSession != null)
            {
                foreach (var provider in this.providers)
                {
                    if (provider.ProviderGuid != Guid.Empty)
                    {
                        this.Logger.LogInformation("Disabling ETW Provider {providerGuid}", provider.ProviderName);
                        this.traceEventSession.DisableProvider(provider.ProviderGuid);
                    }
                    else
                    {
                        this.Logger.LogInformation("Disabling ETW Provider {providerName}", provider.ProviderName);
                        this.traceEventSession.DisableProvider(provider.ProviderName);
                    }
                }
                foreach (var provider in newProviders)
                {
                    if (provider.ProviderGuid != Guid.Empty)
                    {
                        this.Logger.LogInformation("Enabling ETW Provider {providerGuid}", provider.ProviderGuid);
                        this.traceEventSession.EnableProvider(provider.ProviderGuid, provider.Level, (ulong)provider.Keywords);
                    }
                    else
                    {
                        this.Logger.LogInformation("Enabling ETW Provider {providerName}", provider.ProviderName);
                        this.traceEventSession.EnableProvider(provider.ProviderName, provider.Level, (ulong)provider.Keywords);
                    }
                }
            }
            
            this.providers = newProviders;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            if (this.disposedValue)
            {
                throw new ObjectDisposedException(nameof(EtwService));
            }

            if (this.traceEventSession != null)
            {
                throw new InvalidOperationException($"{nameof(EtwService)} has already been activated");
            }

            if (this.providers.Count() == 0)
            {
                return;
            }

            this.traceEventSession = SessionFactory();
            foreach (var provider in this.providers)
            {
                if (provider.ProviderGuid != Guid.Empty)
                {
                    this.Logger.LogInformation("Enabling ETW Provider {providerGuid}", provider.ProviderGuid);
                    this.traceEventSession.EnableProvider(provider.ProviderGuid, provider.Level, (ulong)provider.Keywords);
                }
                else
                {
                    this.Logger.LogInformation("Enabling ETW Provider {providerName}", provider.ProviderName);
                    this.traceEventSession.EnableProvider(provider.ProviderName, provider.Level, (ulong)provider.Keywords);
                }
            }

            this.traceEventSession.EnableProvider(TraceEventExtensions.TplEventSourceGuid, TraceEventLevel.Always, TraceEventExtensions.TaskFlowActivityIdsKeyword);
            await Task.Run(() =>
            {
                try
                {
                    this.traceEventSession.Process(this.eventBuffer.Add);
                }
                catch (Exception e)
                {
                    this.Logger.LogError("ETW session has terminated unexpectedly and events are no longer collected. {exception}", e);
                }
            }, token);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    this.traceEventSession?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this.traceEventSession = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
