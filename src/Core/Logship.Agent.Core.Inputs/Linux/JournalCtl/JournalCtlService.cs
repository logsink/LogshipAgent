using Logship.Agent.Core.Events;
using Logship.Agent.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Logship.Agent.Core.Inputs.Linux.JournalCtl
{
    internal class JournalCtlService : BaseConfiguredService
    {
        private readonly IEventBuffer buffer;
        private int flags;
        private HashSet<string> columns;

        private class ThreadParams
        {
            internal readonly CancellationToken token;
            internal readonly int flags;
            internal readonly IEventBuffer buffer;

            public ThreadParams(CancellationToken token, int flags, IEventBuffer buffer)
            {
                this.token = token;
                this.flags = flags;
                this.buffer = buffer;
            }
        }

        public JournalCtlService(IEventBuffer buffer, ILogger logger) : base(nameof(JournalCtlService), logger)
        {
            this.buffer = buffer;
            this.columns = new HashSet<string>();
        }

        public override void UpdateConfiguration(IConfigurationSection configuration)
        {
            this.flags = configuration.GetInt(nameof(flags), 0, this.Logger);
            var defaultColumns = new HashSet<string>()
            {
                "MESSAGE",
                "PRIORITY",
                "SYSLOG_IDENTIFIER",
                "SYSLOG_FACILITY",
                "SYSLOG_PID",
                "SYSLOG_TIMESTAMP",
                "SYSTEMD_UNIT",
                "SOURCE_REALTIME_TIMESTAMP",
            };

            foreach (var column in configuration.GetValues(nameof(columns), this.Logger))
            {
                defaultColumns.Add(column);
            }

            this.columns = defaultColumns;
        }

        protected override Task ExecuteAsync(CancellationToken token)
        {
            if (false == OperatingSystem.IsLinux())
            {
                this.Logger.LogWarning($"Invalid configuration to execute {nameof(JournalCtlService)} in a non-Linux environment.");
                return Task.CompletedTask;
            }

            RunSync(token);
            return Task.CompletedTask;
        }

        void RunSync(CancellationToken token)
        {
            using var journal = JournalHandle.Open(this.flags);
            int seekResult = Interop.sd_journal_seek_tail(journal.DangerousGetHandle());
            if (seekResult < 0)
            {
                Interop.Throw(seekResult, "Error during sd_journal_seek_tail");
            }

            int result;
            int count = 0;
            do
            {
                result = Interop.sd_journal_next(journal.DangerousGetHandle());
                switch (result)
                {
                    case 1:
                        count++;
                        ReadEntry(journal, token);
                        break;
                    case 0:
                        if (count > 0)
                        {
                            this.Logger.LogInformation("JournalCtl blocking. Last flush was {count} entries.", count);
                            count = 0;
                        }
                        WaitForJournal(journal, token);
                        break;
                    default:
                        Interop.Throw(result, "Error during sd_journal_next");
                        break;
                }
            } while (false == token.IsCancellationRequested);
        }

        private void WaitForJournal(JournalHandle handle, CancellationToken token)
        {
            int r;
            do
            {
                r = Interop.sd_journal_wait(handle.DangerousGetHandle(), 5_000_000);
                if (r < 0)
                {
                    Interop.Throw(r, "Failed to wait for new journal entry");
                }
            } while (false == token.IsCancellationRequested && r == 0);
        }

        void ReadEntry(JournalHandle journal, CancellationToken cancellationToken)
        {
            var fields = new Dictionary<string, object>
            {
                { "machine", Environment.MachineName },
            };

            var extraData = new Dictionary<string, string>();
            int result;
            do
            {
                result = Interop.sd_journal_enumerate_available_data(journal.DangerousGetHandle(), out var data_ptr, out var data_size);
                if (result < 0)
                {
                    Interop.Throw(result, "Error while reading journal entry.");
                }
                else if (result == 0)
                {
                    break;
                }


                // Convert the field name to a string
                string field = Marshal.PtrToStringUTF8((IntPtr)data_ptr, (int)data_size);
                var parts = field.Split(new char[] { '=' }, 2);
                if (parts.Length != 2)
                {
                    continue;
                }

                if (this.columns.Contains(parts[0]))
                {
                    fields[parts[0]] = parts[1];
                }
                else
                {
                    extraData[parts[0]] = parts[1];
                }
            } while (false == cancellationToken.IsCancellationRequested);
            
            fields["ExtraData"] = JsonSerializer.Serialize(extraData, JournalCtlSourceGenerationContext.Default.DictionaryStringString);
            this.buffer.Add(new Records.DataRecord("Linux.JournalD", DateTimeOffset.UtcNow, fields));
        }
    }
}
