using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Logship.Agent.Service.Collectors
{
    /// <summary>
    /// The base asynchronous service runner.
    /// </summary>
    internal abstract class BaseAsyncService
    {
        private readonly string serviceName;

        private CancellationTokenSource? tokenSource = null;
        private CancellationToken stopToken = default;

        private Task? executionTask = null;

        protected BaseAsyncService(
            string serviceName,
            ILogger logger)
        {
            this.serviceName = serviceName;
            this.Logger = logger;
        }

        /// <summary>
        /// Gets the default logger to use.
        /// </summary>
        protected ILogger Logger { get; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.executionTask != null && false == this.executionTask.IsCompleted)
            {
                Environment.FailFast($"Critical invalid operation while starting task: {this.serviceName}. This task is already running.");
                throw new Exception();
            }

            this.executionTask = Task.Run(this.ExecuteAsyncWrapper);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            this.stopToken = cancellationToken;
            this.tokenSource?.Cancel();
            var execution = this.executionTask;
            if (null != execution)
            {
                await execution;
            }
        }

        private async Task ExecuteAsyncWrapper()
        {
            using var activity = new Activity("Exec-" + this.serviceName);
            using var scope = this.Logger.BeginScope("");
            var tokenSource = new CancellationTokenSource();
            this.tokenSource = tokenSource;
            var token = this.tokenSource.Token;

            try
            {
                this.Logger.LogInformation("Starting service: {name}", this.serviceName);
                await this.OnStart(token);
                this.Logger.LogInformation("Finished starting service: {name}", this.serviceName);
            }
            catch (Exception ex)
            {
                this.Logger.LogCritical($"Failed to start service {this.serviceName} with exception {ex}");
                Environment.FailFast($"Failed to start service {this.serviceName} with exception {ex}");
            }

            try
            {
                this.Logger.LogInformation("Begining runner loop for service: {name}", this.serviceName);
                await this.ExecuteAsync(token);
                this.Logger.LogInformation("Finished runner loop for service: {name}", this.serviceName);
            }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested && ex is OperationCanceledException)
                {
                    // This is ok. 
                }
                else
                {
                    this.Logger.LogCritical($"Exception thrown to runner in service {this.serviceName}. {ex}");
                    Environment.FailFast($"Exception thrown to runner in service {this.serviceName}. {ex}");
                }
            }

            try
            {
                await this.OnStop(this.stopToken);
            }
            catch (Exception ex)
            {
                this.Logger.LogCritical($"Exception thrown during shutdown in service {this.serviceName}. {ex}");
                Environment.FailFast($"Exception thrown during shutdown in service {this.serviceName}. {ex}");
            }

            this.Logger.LogInformation("Successfully stopped service: {name}", this.serviceName);
        }

        protected virtual Task OnStart(CancellationToken token) => Task.CompletedTask;

        protected virtual Task OnStop(CancellationToken token) => Task.CompletedTask;

        /// <summary>
        /// Implement on execution.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        protected abstract Task ExecuteAsync(CancellationToken token);
    }
}
