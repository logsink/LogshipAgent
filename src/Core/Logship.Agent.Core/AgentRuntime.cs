using Logship.Agent.Core.Events;
using Logship.Agent.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;

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
        private readonly IDictionary<string, BaseAsyncService> services;
        private BaseAsyncService eventOutput;

        internal AgentRuntime(IConfigurationRoot configuration, ILoggerFactory loggerFactory, ConcurrentDictionary<string, Func<IEventSink, ILogger, BaseAsyncService>> configuredInputs)
        {
            this.configuredInputs = configuredInputs;
            this.configuration = configuration;
            this.reloadToken = configuration.GetReloadToken();
            this.cbDisposable = this.reloadToken.RegisterChangeCallback(ConfigurationChanged, this);

            this.loggerFactory = loggerFactory;
            this.rootLogger = loggerFactory.CreateLogger(nameof(AgentRuntime));
            this.services = new Dictionary<string, BaseAsyncService>();

            string endpoint = configuration.GetSection("Output").GetRequiredString(nameof(endpoint), this.rootLogger);
            Guid subscription = configuration.GetSection("Output").GetRequiredGuid(nameof(subscription), this.rootLogger);
            int maximumBufferSize = configuration.GetSection("Output").GetInt(nameof(maximumBufferSize), 15_000, this.rootLogger);
            int maximumFlushSize = configuration.GetSection("Output").GetInt(nameof(maximumFlushSize), 15_000, this.rootLogger);

            IEventOutput output = endpoint == "console"
                ? new ConsoleEventOutput(loggerFactory.CreateLogger(nameof(ConsoleEventOutput)))
                : new LogshipEventOutput(endpoint, subscription, loggerFactory.CreateLogger(nameof(LogshipEventOutput)));
            this.eventSink = new EventSink(
                maximumFlushSize,
                output,
                new InMemoryBuffer(maximumBufferSize, loggerFactory.CreateLogger(nameof(InMemoryBuffer))),
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
                    lock (this.services)
                    {
                        foreach (var service in services)
                        {
                            if (service.Value is IDisposable d)
                            {
                                rootLogger.LogInformation("Disposing service {serviceName}.", service.Key);
                                d.Dispose();
                                rootLogger.LogInformation("Disposing service {serviceName}.", service.Key);
                            }
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
            this.LoadConfiguredServices();
            IEnumerable<KeyValuePair<string, BaseAsyncService>> snapshot;
            lock (this.services)
            {
                snapshot = this.services.ToList();
            }

            foreach (var service in snapshot)
            {
                rootLogger.LogInformation("Starting service {serviceName}.", service.Key);
                await service.Value.StartAsync(cancellationToken);
                rootLogger.LogInformation("Started service {serviceName}.", service.Key);
            }
        }

        private void LoadConfiguredServices()
        {
            var inputs = this.configuration.GetSection("Inputs");
            foreach (var inputConfig in inputs.GetChildren())
            {
                string? type = inputConfig["Type"]?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(type))
                {
                    continue;
                }

                lock (this.services)
                {
                    if (false == this.services.TryGetValue(type, out BaseAsyncService? service))
                    {
                        service = CreateServiceFromConfig(type, inputConfig);
                        if (service == null)
                        {
                            this.rootLogger.LogWarning("Invalid configuration for input type {input}.", type);
                            continue;
                        }
                        this.services.Add(type, service);
                    }

                    if (service is BaseConfiguredService c)
                    {
                        c.UpdateConfiguration(inputConfig);
                    }
                }
            }

            var output = this.configuration.GetSection("Output");
            lock (this.services)
            {
                if (false == this.services.TryGetValue("Output", out var existingOutput))
                {
                    existingOutput = new PushService(this.eventSink, loggerFactory.CreateLogger(nameof(PushService)));
                    this.services.Add("Output", existingOutput);
                }
                ((PushService)existingOutput).UpdateConfiguration(output);
            }
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
            this.LoadConfiguredServices();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            IEnumerable<KeyValuePair<string, BaseAsyncService>> snapshot;
            lock (this.services)
            {
                snapshot = this.services.ToList();
            }

            foreach (var service in snapshot)
            {
                rootLogger.LogInformation("Stopping service {serviceName}.", service.Key);
                await service.Value.StopAsync(cancellationToken);
                rootLogger.LogInformation("Stopping service {serviceName}.", service.Key);
            }
        }
    }
}
