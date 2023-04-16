using Logship.Agent.Core.Internals;
using Logship.Agent.Core.Records;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Logship.Agent.Core.Events
{
    internal class EventSink : IEventSink, IEventBuffer, IEventOutput, IDisposable
    {
        private readonly IEventBuffer buffer;
        private readonly ILogger logger;
        private readonly IEventOutput eventOutput;
        private bool disposedValue;

        public EventSink(IEventOutput eventOutput, IEventBuffer buffer, ILogger logger)
        {
            this.buffer = buffer;
            this.logger = Throw.IfArgumentNull(logger, nameof(logger));
            this.eventOutput = Throw.IfArgumentNull(eventOutput, nameof(eventOutput));
        }

        public async Task FlushAsync(CancellationToken token)
        {
            IReadOnlyCollection<DataRecord> records = await this.buffer.NextAsync(token);
            if (records.Count == 0)
            {
                return;
            }

            this.logger.LogInformation("Flushing {flushSize} data records.", records.Count);
            try
            {
                foreach (var batch in records.Chunk(100_000))
                {
                    using var flush = new EventSinkFlushContext(batch, onFailure: this.buffer.Add, logger);
                    flush.Success = await this.eventOutput.SendAsync(batch, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { /* noop */ }
            catch (Exception ex)
            {
                this.logger.LogError("An exception occurred during flush: {exception}", ex);
                throw;
            }
        }

        class EventSinkFlushContext : IDisposable
        {
            private readonly ILogger logger;
            private readonly Action<IReadOnlyCollection<DataRecord>> onFailure;
            private readonly IReadOnlyCollection<DataRecord> records;
            private bool disposedValue;

            /// <summary>
            /// Whether this flush was successful.
            /// </summary>
            public bool Success { get; set; }

            internal EventSinkFlushContext(IReadOnlyCollection<DataRecord> records, Action<IReadOnlyCollection<DataRecord>> onFailure, ILogger logger)
            {
                this.records = records;
                this.onFailure = onFailure;
                this.logger = logger;
                this.Success = false;
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        if (false == this.Success)
                        {
                            this.logger.LogWarning("EventSink flush failed. Re-inserting {count} records.", records.Count);
                            this.onFailure(this.records);
                        }
                    }

                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.eventOutput is IDisposable eventD)
                    {
                        eventD.Dispose();
                    }

                    if (this.buffer is IDisposable bufferD)
                    {
                        bufferD.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public Task<bool> SendAsync(IReadOnlyCollection<DataRecord> records, CancellationToken cancellationToken)
        {
            return eventOutput.SendAsync(records, cancellationToken);
        }

        public void Add(DataRecord data)
        {
            buffer.Add(data);
        }

        public void Add(IReadOnlyCollection<DataRecord> data)
        {
            buffer.Add(data);
        }

        public Task<IReadOnlyCollection<DataRecord>> NextAsync(CancellationToken token)
        {
            return buffer.NextAsync(token);
        }
    }
}
