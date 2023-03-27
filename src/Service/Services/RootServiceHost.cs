using Logship.Agent.Service.Collectors;
using Logship.Agent.Service.Configuration;
using Microsoft.Extensions.Logging;

namespace Logship.Agent.Service.Services
{
    internal class RootServiceHost : BaseAsyncService
    {
        private readonly InfoSink infoSink = new InfoSink();
        private readonly RootConfiguration rootConfiguration;
        private readonly PushService pushService;
        private UptimeService? uptimeService;

        // Windows
        private WindowsPerformanceCountersService? windowsPerformanceCountersService;

        public RootServiceHost(RootConfiguration rootConfiguration, ILogger logger) : base("RootService", logger)
        {
            this.rootConfiguration = rootConfiguration;
            this.pushService = new PushService(this.rootConfiguration.PushService, this.infoSink, logger);
        }

        protected override async Task OnStart(CancellationToken token)
        {
            await this.pushService.StartAsync(token);
            await base.OnStart(token);
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (false == token.IsCancellationRequested)
            {
                this.uptimeService = await this.StartStopBasedOnConfig(this.rootConfiguration.UptimeService, this.uptimeService, () => new UptimeService(this.rootConfiguration.UptimeService, this.infoSink, this.Logger), token);

                if (this.rootConfiguration.Windows.Enabled)
                {
                    // windows
                    this.windowsPerformanceCountersService = await this.StartStopBasedOnConfig(this.rootConfiguration.Windows.PerformanceCounter, this.windowsPerformanceCountersService, () => new WindowsPerformanceCountersService(this.rootConfiguration.Windows.PerformanceCounter, this.infoSink, this.Logger), token);
                }

                await Task.Delay(5000, token);
            }
        }

        protected override async Task OnStop(CancellationToken token)
        {
            await this.StopIfRunning(this.uptimeService, token);
            await this.StopIfRunning(this.windowsPerformanceCountersService, token);

            await this.pushService.StopAsync(token);
            await base.OnStop(token);
        }

        private async Task<T> StartStopBasedOnConfig<T>(BaseServiceConfiguration config, T? existing, Func<T> startServiceTask, CancellationToken token)
            where T : BaseAsyncService
        {
            if (null == existing)
            {
                if (config.Enabled)
                {
                    var service = startServiceTask();
                    await service.StartAsync(token);
                    return service;
                }
            }
            else
            {
                if (false == config.Enabled)
                {
                    await existing.StopAsync(token);
                    return null!;
                }
            }

            return existing!;
        }

        private async Task StopIfRunning(BaseAsyncService? service, CancellationToken token)
        {
            if (null != service)
            {
                await service.StopAsync(token);
            }
        }
    }
}
