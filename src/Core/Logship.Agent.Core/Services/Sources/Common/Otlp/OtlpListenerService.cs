using Grpc.Core;
using Logship.Agent.Core.Configuration;
using Logship.Agent.Core.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Logship.Agent.Core.Services.Sources.Common.Otlp
{
    public sealed class OtlpListenerService : BaseInputService<OtlpConfiguration>
    {
        private readonly Grpc.Core.Server server;

        public OtlpListenerService(IOptions<SourcesConfiguration> config, IEventBuffer buffer, ILoggerFactory logger)
            : base(config.Value.Otlp, buffer, nameof(OtlpListenerService), logger.CreateLogger<OtlpListenerService>())
        {
            var extractResourceAttributes = new Dictionary<string, ExtractResourceAttribute>()
            {
                ["ServiceVersion"] = new ExtractResourceAttribute("service.version", string.Empty),
                ["ServiceName"] = new ExtractResourceAttribute("service.name", "unknown_service"),
            };

            server = new Grpc.Core.Server();
            server.Services.Add(LogsService.BindService(new OtlpLogsGrpcService(extractResourceAttributes, buffer, logger.CreateLogger<OtlpLogsGrpcService>())));
            server.Services.Add(MetricsService.BindService(new OtlpMetricsGrpcService(extractResourceAttributes, buffer, logger.CreateLogger<OtlpMetricsGrpcService>())));
            server.Services.Add(TraceService.BindService(new OtlpTraceGrpcService(extractResourceAttributes, buffer, logger.CreateLogger<OtlpTraceGrpcService>())));
            server.Ports.Add(new ServerPort("localhost", this.Config.Port, ServerCredentials.Insecure));
            Log.CreatedGrpcEndpoint(this.Logger, this.Config.Port);
        }

        protected override Task ExecuteAsync(CancellationToken token)
        {
            this.server.Start();
            return Task.Delay(-1, token);
        }

        protected override async Task OnStop(CancellationToken token)
        {
            await base.OnStop(token);
            await this.server.ShutdownAsync();
        }
    }

    internal static partial class Log
    {
        [LoggerMessage(LogLevel.Information, "Created OTLP grpc host on port: {Port}")]
        public static partial void CreatedGrpcEndpoint(ILogger logger, int port);

    }
}
