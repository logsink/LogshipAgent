using Logship.Agent.Core.Events;
using Logship.Agent.Core.Internals;
using Logship.Agent.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logship.Agent.Core
{
    public class AgentRuntimeFactory
    {
        private readonly ILogger logger;
        private readonly ConcurrentDictionary<string, Func<IEventSink, ILogger, BaseAsyncService>> configuredInputs;

        public AgentRuntimeFactory(IConfigurationRoot configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            LoggerFactory = loggerFactory;
            this.configuredInputs = new ConcurrentDictionary<string, Func<IEventSink, ILogger, BaseAsyncService>>();
            this.logger = loggerFactory.CreateLogger<AgentRuntimeFactory>();
            this.RegisterCoreInputs();
        }

        public IConfigurationRoot Configuration { get; }
        public ILoggerFactory LoggerFactory { get; }

        public AgentRuntimeFactory RegisterInputService(string configName, Func<IEventBuffer, ILogger, BaseAsyncService> createServiceFunc)
        {
            configName = Throw.IfArgumentNullOrWhiteSpace(configName, nameof(configName)).ToLowerInvariant();

            this.logger.LogInformation("Registering configured input service {serviceName}", configName);
            if (this.configuredInputs.TryRemove(configName, out var input))
            {
                this.logger.LogWarning("Service {serviceName} was already configured. Overwriting.", configName);
            }

            this.configuredInputs[configName] = createServiceFunc;
            return this;
        }

        protected void RegisterCoreInputs()
        {
            this.RegisterInputService(HealthService.ConfigTypeName, (s, l) => new HealthService(s, l));
        }

        public IAgentRuntime Build()
        {
            return new AgentRuntime(this.Configuration, this.LoggerFactory, configuredInputs);
        }
    }
}
