using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Diagnostics.Tracing;
using Logship.Agent.Core.Records;

namespace Logship.Agent.Core.Inputs.Windows.Etw
{
    internal class StandardTraceEventSession : ITraceEventSession
    {
        private TraceEventSession inner;
        private volatile bool isProcessing;

        public StandardTraceEventSession(string sessionNamePrefix, bool cleanupOldSessions, bool reuseExisting)
        {
            string? sessionName = null;
            if (cleanupOldSessions)
            {
                sessionName = CleanupMatchingSessions(sessionNamePrefix, reuseExisting);
            }

            sessionName ??= $"{sessionNamePrefix}-{Guid.NewGuid()}";

            // even if the session already exists, we must restart it as we cannot enable providers on an attached session
            this.inner = new TraceEventSession(sessionName, TraceEventSessionOptions.Create);
            this.isProcessing = false;
        }

        public void DisableProvider(Guid providerGuid)
        {
            if (this.inner == null)
            {
                throw new ObjectDisposedException(nameof(StandardTraceEventSession));
            }

            this.inner.DisableProvider(providerGuid);
        }

        public void DisableProvider(string providerName)
        {
            if (this.inner == null)
            {
                throw new ObjectDisposedException(nameof(StandardTraceEventSession));
            }

            this.inner.DisableProvider(providerName);
        }

        public void Dispose()
        {
            if (this.inner != null)
            {
                // TraceEventSession.StopOnDispose is true by default so there is no need to Stop() the session explicitly.
                this.inner.Dispose();
                this.inner = null;
            }
        }

        public void EnableProvider(Guid providerGuid, TraceEventLevel maximumEventLevel, ulong enabledKeywords)
        {
            if (this.inner == null)
            {
                throw new ObjectDisposedException(nameof(StandardTraceEventSession));
            }

            this.inner.EnableProvider(providerGuid, maximumEventLevel, enabledKeywords);
        }

        public void EnableProvider(string providerName, TraceEventLevel maximumEventLevel, ulong enabledKeywords)
        {
            if (this.inner == null)
            {
                throw new ObjectDisposedException(nameof(StandardTraceEventSession));
            }

            this.inner.EnableProvider(providerName, maximumEventLevel, enabledKeywords);
        }

        public void Process(Action<DataRecord> onEvent)
        {
            if (this.inner == null)
            {
                throw new ObjectDisposedException(nameof(StandardTraceEventSession));
            }

            if (onEvent == null)
            {
                throw new ArgumentNullException(nameof(onEvent));
            }

            if (!isProcessing)
            {
                isProcessing = true;
                this.inner.Source.Dynamic.All += (traceEvent) =>
                {
                    // Suppress events from TplEventSource--they are mostly interesting for debugging task processing and interaction,
                    // and not that useful for production tracing. However, TPL EventSource must be enabled to get hierarchical activity IDs.
                    if (!TraceEventExtensions.TplEventSourceGuid.Equals(traceEvent.ProviderGuid))
                    {
                        onEvent(traceEvent.ToEventData());
                    }
                };
                this.inner.Source.Process();
            }
        }

        private string CleanupMatchingSessions(string sessionNamePrefix, bool keepOne)
        {
            string result = null;

            foreach (var sesName in TraceEventSession.GetActiveSessionNames())
            {
                if (!sesName.StartsWith(sessionNamePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var session = TraceEventSession.GetActiveSession(sesName);
                if (session == null || !session.IsRealTime)
                {
                    continue;
                }

                if (keepOne && result == null)
                {
                    result = session.SessionName;
                }
                else
                {
                    session.Dispose();
                }
            }

            return result;
        }
    }
}
