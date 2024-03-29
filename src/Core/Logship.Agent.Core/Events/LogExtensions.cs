using Microsoft.Extensions.Logging;

namespace Logship.Agent.Core.Events
{
    internal static partial class EventsLog
    {
        [LoggerMessage(LogLevel.Information, "Sending events to output {Endpoint} using subscription {Subscription}")]
        public static partial void Endpoint(ILogger logger, string endpoint, Guid subscription);

        [LoggerMessage(LogLevel.Information, "Maximum buffer size: {maximumBufferSize}")]
        public static partial void BufferSize(ILogger logger, int maximumBufferSize);
    }
}
