using Logship.Agent.Core.Records;
using Microsoft.Diagnostics.Tracing;

namespace Logship.Agent.Core.Inputs.Windows.Etw
{
    public interface ITraceEventSession : IDisposable
    {
        void EnableProvider(Guid providerGuid, TraceEventLevel maximumEventLevel, ulong enabledKeywords);
        void DisableProvider(Guid providerGuid);
        void EnableProvider(string providerName, TraceEventLevel maximumEventLevel, ulong enabledKeywordsOptions);
        void DisableProvider(string providerName);
        void Process(Action<DataRecord> onEvent);
    }
}
