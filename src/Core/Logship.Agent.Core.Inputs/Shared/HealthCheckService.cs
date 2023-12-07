using Logship.Agent.Core.Events;
using Logship.Agent.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Logship.Agent.Core.Inputs.Shared
{
    internal class HealthCheckService : BaseConfiguredService
    {
        public static string ConfigTypeName => "healthcheck";

        private readonly object _mutex = new object();
        private readonly IEventBuffer sink;
        private List<HealthCheckTarget> targets;
        private readonly List<Task<HealthCheckState>> healthChecks;
        private bool updateTargets = true;

        public HealthCheckService(IEventBuffer sink, ILogger logger)
            : base(nameof(HealthCheckService), logger)
        {
            this.sink = sink;;
            this.targets = new List<HealthCheckTarget>();
            this.healthChecks = new List<Task<HealthCheckState>>();
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (false == token.IsCancellationRequested)
            {
                if (this.healthChecks.Count == 0 && false == updateTargets)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), token);
                    continue;
                }

                lock (_mutex)
                {
                    if (updateTargets)
                    {
                        updateTargets = false;
                        Logger.LogInformation("Updating HealthCheck Targets");
                        foreach (var target in targets)
                        {
                            this.healthChecks.Add(PerformHealthCheck(target, token));
                        }
                    }
                    
                }

                try
                {
                    var completedTask = await Task.WhenAny(this.healthChecks);
                    var state = await completedTask;
                    
                    lock (_mutex)
                    {
                        this.healthChecks.Remove(completedTask);
                        var duration = DateTime.UtcNow - state.Start;
                        if (duration <= TimeSpan.Zero)
                        {
                            duration = TimeSpan.Zero;
                        }

                        this.healthChecks.Add(PerformHealthCheckWithDelay(state.Target, state.Target.Interval - duration, token));
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, ex.Message);
                }
            }
        }

        private async Task<HealthCheckState> PerformHealthCheckWithDelay(HealthCheckTarget target, TimeSpan delay, CancellationToken token)
        {
            await Task.Delay(delay, token);
            return await PerformHealthCheck(target, token);
        }

        private async Task<HealthCheckState> PerformHealthCheck(HealthCheckTarget target, CancellationToken token)
        {
            using var client = new HttpClient();
            var start = DateTime.UtcNow;
            Logger.LogInformation("Starting HealthCheck on {Endpoint}", target.Endpoint.ToString());

            HealthCheckState state;
            try
            {
                using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, target.Endpoint), token);
                var done = DateTime.UtcNow;
                state = new HealthCheckState(target, start, done, response);
                await PushResponse(state, token);
                Logger.LogInformation("Finished HealthCheck on {Endpoint}", target.Endpoint.ToString());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                state = new HealthCheckState(target, start, DateTime.UtcNow, ex);
                await PushException(state, token);
            }

            return state;
        }

        private async Task PushResponse(HealthCheckState state, CancellationToken token)
        {
            string headers = state.ResponseMessage != null && state.Target.IncludeResponseHeaders
                ? JsonSerializer.Serialize(state.ResponseMessage.Headers, HealthServiceSerializerContext.Default.HttpResponseHeaders)
                : string.Empty;
            string body = state.ResponseMessage != null && state.Target.IncludeResponseHeaders
                ? await state.ResponseMessage.Content.ReadAsStringAsync(token)
                : string.Empty;
            this.sink.Add(new Records.DataRecord("Logship.Agent.HealthChecks", DateTimeOffset.UtcNow, new Dictionary<string, object>
            {
                { "machine", Environment.MachineName },
                { "endpoint", state.Target.Endpoint.ToString() },
                { "startTimeUtc", state.Start.ToString("O") },
                { "timestamp", state.End.ToString("O") },
                { "interval", state.Target.Interval.ToString("c") },
                { "status", ((int?)state.ResponseMessage?.StatusCode) ?? -1 },
                { "latencyMillis", (int)(state.End - state.Start).TotalMilliseconds },
                { "headers", headers },
                { "body", body },
            }));
        }

        private Task PushException(HealthCheckState state, CancellationToken token)
        {
            this.sink.Add(new Records.DataRecord("Logship.Agent.HealthChecks.Exceptions", DateTimeOffset.UtcNow, new Dictionary<string, object>
            {
                { "machine", Environment.MachineName },
                { "endpoint", state.Target.Endpoint.ToString() },
                { "startTimeUtc", state.Start.ToString("O") },
                { "timestamp", state.End.ToString("O") },
                { "interval", state.Target.Interval.ToString("c") },
                { "message", state.Exception!.Message },
                { "exception", state.Exception.ToString() },
            }));

            return Task.CompletedTask;
        }

        public override void UpdateConfiguration(IConfigurationSection configuration)
        {
            var targetsSection = configuration.GetSection(nameof(this.targets));
            var newProviders = new List<HealthCheckTarget>();
            foreach (var provider in targetsSection.GetChildren())
            {
                if (HealthCheckTarget.TryFrom(provider, this.Logger, out var check))
                {
                    newProviders.Add(check);
                }
            }

            lock (_mutex)
            {
                this.targets = newProviders;
            }

            updateTargets = true;
        }

        private class HealthCheckState
        {
            public HealthCheckTarget Target;
            public DateTime Start;
            public DateTime End;
            public HttpResponseMessage? ResponseMessage;
            public Exception? Exception;

            public HealthCheckState(HealthCheckTarget target, DateTime start, DateTime end, HttpResponseMessage? responseMessage)
            {
                Target = target;
                Start = start;
                End = end;
                ResponseMessage = responseMessage;
                Exception = null!;
            }

            public HealthCheckState(HealthCheckTarget target, DateTime start, DateTime end, Exception? exception)
            {
                Target = target;
                Start = start;
                End = end;
                ResponseMessage = null!;
                Exception = exception;
            }
        }
    }

    [JsonSerializable(typeof(HttpResponseHeaders))]
    internal partial class HealthServiceSerializerContext : JsonSerializerContext
    {

    }

    class HealthCheckTarget
    {
        public HealthCheckTarget(Uri endpoint, TimeSpan interval, bool includeResponseBody, bool includeResponseHeaders)
        {
            Endpoint = endpoint;
            Interval = interval;
            IncludeResponseBody = includeResponseBody;
            IncludeResponseHeaders = includeResponseHeaders;
        }

        public Uri Endpoint { get; set; }

        public TimeSpan Interval { get; set; }

        public bool IncludeResponseHeaders { get; set; }

        public bool IncludeResponseBody { get; set; }

        public static bool TryFrom(IConfigurationSection configuration, ILogger logger, [NotNullWhen(true)] out HealthCheckTarget? result)
        {
            result = null;
            if (false == Uri.TryCreate(configuration.GetRequiredString(nameof(Endpoint), logger), UriKind.Absolute, out var uri)) {
                return false;
            }

            result = new HealthCheckTarget(
                endpoint: uri,
                interval: configuration.GetTimeSpan(nameof(Interval), TimeSpan.FromSeconds(60), logger)!,
                includeResponseBody: configuration.GetBool(nameof(IncludeResponseBody), false, logger),
                includeResponseHeaders: configuration.GetBool(nameof(IncludeResponseHeaders), false, logger));
            return result.Interval > TimeSpan.Zero;
        }
    }
}
