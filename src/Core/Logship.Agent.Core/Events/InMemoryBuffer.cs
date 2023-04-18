using Logship.Agent.Core.Records;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace Logship.Agent.Core.Events
{
    internal class InMemoryBuffer : IEventBuffer
    {
        private volatile ConcurrentBag<DataRecord> bag;

        public InMemoryBuffer(ILogger logger)
        {
            this.bag = new ConcurrentBag<DataRecord>();
        }

        public void Add(DataRecord data)
        {
            bag.Add(data);
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

            var temp = this.bag;
            this.bag = new ConcurrentBag<DataRecord>();
            var items = temp.ToArray();
            temp.Clear();
            return Task.FromResult<IReadOnlyCollection<DataRecord>>(items);
        }
    }
}
