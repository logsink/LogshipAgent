using Logship.Agent.Core.Records;
using Microsoft.Extensions.Logging;

namespace Logship.Agent.Core.Events
{
    internal class InMemoryBuffer : IEventBuffer
    {
        private List<DataRecord> bag;
        private int maximumBufferSize;
        private readonly ILogger logger;
        private readonly object mutex = new();

        public InMemoryBuffer(int maximumBufferSize, ILogger logger)
        {
            this.bag = new List<DataRecord>(maximumBufferSize);
            this.maximumBufferSize = maximumBufferSize;
            this.logger = logger;
        }

        public void Add(DataRecord data)
        {
            bool added = false;
            lock (mutex) 
            {
                if (bag.Count < maximumBufferSize)
                {
                    bag.Add(data);
                    added = true;
                }
            }

            if (false == added)
            {
                this.logger.LogWarning($"{nameof(DataRecord)} dropped. Consider increasing {nameof(maximumBufferSize)}: {{maximumBufferSize}} records", maximumBufferSize);
            }
        }

        public void Add(IReadOnlyCollection<DataRecord> data)
        {
            foreach (var item in data)
            {
                bag.Add(item);
            }
        }

        public Task<IReadOnlyCollection<DataRecord>> NextAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return Task.FromResult<IReadOnlyCollection<DataRecord>>(Array.Empty<DataRecord>());
            }

            DataRecord[] items;
            lock (mutex)
            {
                items = this.bag.ToArray();
                this.bag.Clear();
            }

            return Task.FromResult<IReadOnlyCollection<DataRecord>>(items);
        }
    }
}
