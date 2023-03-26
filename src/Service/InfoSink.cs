using Logship.Agent.Service.Records;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Logship.Agent.Service
{
    internal sealed class InfoSink
    {
        private ConcurrentBag<DataRecord> currentData = new ConcurrentBag<DataRecord>();

        public void Push(DataRecord record)
        {
            this.currentData.Add(record);
        }

        public ExtractRecordsContainer GetNextBatch()
        {
            var newSet = new ConcurrentBag<DataRecord>();
            var oldSet = this.currentData;
            this.currentData = newSet;

            return new ExtractRecordsContainer(oldSet, a =>
            {
                foreach (var item in a)
                {
                    newSet.Add(item);
                }
            });
        }

        
        public class ExtractRecordsContainer : IDisposable
        {
            private readonly Action<IEnumerable<DataRecord>> restore;
            private readonly ConcurrentBag<DataRecord> records;
            private bool flushed = false;

            public ExtractRecordsContainer(
                ConcurrentBag<DataRecord> records,
                Action<IEnumerable<DataRecord>> restore)
            {
                this.records = records;
                this.restore = restore;
            }

            public int Length => this.records.Count;

            public IEnumerable<DataRecord> Records => this.records;

            public void Dispose()
            {
                if (true == this.flushed)
                {
                    return;
                }

                this.restore(this.records);
            }

            public void Success()
            {
                this.flushed = true;
            }


        }
    }
}
