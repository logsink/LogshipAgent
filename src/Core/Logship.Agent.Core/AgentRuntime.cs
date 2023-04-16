using Logship.Agent.Core.Events;
using Logship.Agent.Core.Internals;
using Logship.Agent.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;

namespace Logship.Agent.Core
{
    public class AgentRuntime : IAgentRuntime, IDisposable
    {
        private readonly ConcurrentDictionary<string, Func<IEventSink, ILogger, BaseAsyncService>> configuredInputs;
        private readonly IConfigurationRoot configuration;
        private readonly IChangeToken reloadToken;
        private readonly IDisposable cbDisposable;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger rootLogger;
        private readonly EventSink eventSink;
        private readonly ConcurrentDictionary<string, BaseAsyncService?> services;
        private BaseAsyncService eventOutput;

        internal AgentRuntime(IConfigurationRoot configuration, ILoggerFactory loggerFactory, ConcurrentDictionary<string, Func<IEventSink, ILogger, BaseAsyncService?>> configuredInputs)
        {
            this.configuredInputs = configuredInputs;
            this.configuration = configuration;
            this.reloadToken = configuration.GetReloadToken();
            this.cbDisposable = this.reloadToken.RegisterChangeCallback(ConfigurationChanged, this);

            this.loggerFactory = loggerFactory;
            this.rootLogger = loggerFactory.CreateLogger(nameof(AgentRuntime));
            this.services = new ConcurrentDictionary<string, BaseAsyncService?>();

            string endpoint = configuration.GetSection("Output").GetRequiredStringValue(nameof(endpoint), this.rootLogger);
            IEventOutput output = endpoint == "console"
                ? new ConsoleEventOutput(loggerFactory.CreateLogger(nameof(ConsoleEventOutput)))
                : new LogshipEventOutput(endpoint, loggerFactory.CreateLogger(nameof(LogshipEventOutput)));
            this.eventSink = new EventSink(
                output,
                new InMemoryBuffer(loggerFactory.CreateLogger(nameof(InMemoryBuffer))),
                loggerFactory.CreateLogger(nameof(EventSink)));
            this.eventOutput = new PushService(eventSink, loggerFactory.CreateLogger(nameof(PushService)));
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.cbDisposable.Dispose();
                    foreach(var service in services)
                    {
                        if (service.Value is IDisposable d)
                        {
                            rootLogger.LogInformation("Disposing service {serviceName}.", service.Key);
                            d.Dispose();
                            rootLogger.LogInformation("Disposing service {serviceName}.", service.Key);
                        }
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.LoadConfigureServices();
            foreach (var service in services)
            {
                rootLogger.LogInformation("Starting service {serviceName}.", service.Key);
                await service.Value!.StartAsync(cancellationToken);
                rootLogger.LogInformation("Started service {serviceName}.", service.Key);
            }
        }

        private void LoadConfigureServices()
        {
            var inputs = this.configuration.GetSection("Inputs");
            foreach (var inputConfig in inputs.GetChildren())
            {
                string? type = inputConfig["Type"]?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(type))
                {
                    continue;
                }

                var service = this.services.GetOrAdd(type,
                    t => CreateServiceFromConfig(t, inputConfig));
                if (service == null)
                {
                    this.rootLogger.LogWarning("Invalid configuration for input type {input}.", type);
                    _ = this.services!.Remove(type, out _);
                }
                else if (service is BaseConfiguredService c)
                {
                    c.UpdateConfiguration(inputConfig);
                }
            }

            var output = this.configuration.GetSection("Output");
            var outputService = (PushService) this.services.GetOrAdd("Output",
                t => new PushService(this.eventSink, loggerFactory.CreateLogger(nameof(PushService))))!;
            outputService.UpdateConfiguration(output);
        }

        private BaseAsyncService? CreateServiceFromConfig(string type, IConfigurationSection config)
        {
            if (this.configuredInputs.TryGetValue(type, out Func<IEventSink, ILogger, BaseAsyncService>? inputFactory))
            {
                return inputFactory?.Invoke(this.eventSink, this.loggerFactory.CreateLogger(type));
            }

            return null;
        }

        private void ConfigurationChanged(object? state)
        {
            this.rootLogger.LogInformation("Configuration changed. Updating services.");
            this.configuration.Reload();
            this.LoadConfigureServices();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var service in services)
            {
                rootLogger.LogInformation("Stopping service {serviceName}.", service.Key);
                await service.Value.StopAsync(cancellationToken);
                rootLogger.LogInformation("Stopping service {serviceName}.", service.Key);
            }
        }
    }
}
