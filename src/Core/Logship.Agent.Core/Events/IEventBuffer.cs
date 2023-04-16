using Logship.Agent.Core.Internals;
using Logship.Agent.Core.Records;

namespace Logship.Agent.Core.Events
{
    public interface IEventBuffer
    {
        void Add(DataRecord data);
        void Add(IReadOnlyCollection<DataRecord> data);
        Task<IReadOnlyCollection<DataRecord>> NextAsync(CancellationToken token);
    }
}
