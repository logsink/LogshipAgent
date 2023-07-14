using Logship.Agent.Core.Events;
using Logship.Agent.Core.Records;
using Logship.Agent.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Logship.Agent.Core.Inputs.Linux.Proc
{
    /// <summary>
    /// Reads open file info per process.
    /// </summary>
    internal class ProcFileReaderService : BaseConfiguredService
    {
        private readonly IEventBuffer eventSink;
        private readonly Dictionary<int, ProcPidData> processes;
        private TimeSpan interval = TimeSpan.FromMinutes(2);

        public ProcFileReaderService(IEventBuffer buffer, ILogger logger) : base(nameof(ProcFileReaderService), logger)
        {
            this.eventSink = buffer;
            this.processes = new Dictionary<int, ProcPidData>();
        }

        public override void UpdateConfiguration(IConfigurationSection configuration)
        {
            this.interval = configuration.GetTimeSpan(nameof(this.interval), this.interval, this.Logger);
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            if (false == OperatingSystem.IsLinux())
            {
                this.Logger.LogWarning($"Invalid configuration to execute {nameof(ProcFileReaderService)} in a non-Linux environment.");
                return;
            }

            while (false == token.IsCancellationRequested)
            {
                this.ReadPerProcessFiles();

                await Task.Delay(this.interval, token);
            }
        }

        private void ReadPerProcessFiles()
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var processDirectory in Directory.EnumerateDirectories("/proc"))
            {
                if (false == int.TryParse(Path.GetFileName(processDirectory), out var pid))
                {
                    continue;
                }

                this.Logger.LogTrace("Logging files for process id:{id} {dir}", processDirectory, Path.GetFileName(processDirectory));

                try
                {
                    // Fetch the process name.
                    var procName = File.ReadAllText(Path.Combine(processDirectory, "cmdline")).Split(" ")[0];

                    // Enumerate the open files.
                    foreach (var file in Directory.EnumerateFiles(Path.Combine(processDirectory, "fd")))
                    {
                        var info = new FileInfo(file);
                        if (false == info.Exists || null == info.LinkTarget)
                        {
                            continue;
                        }

                        var fileName = info.LinkTarget;

                        // If the file name ends with (deleted), then record that the file has been deleted, and strip the suffix.
                        var isDeleted = false;
                        if (fileName.EndsWith(" (deleted)"))
                        {
                            isDeleted = true;
                            fileName = file.Substring(0, file.Length - 10);
                        }

                        this.eventSink.Add(new DataRecord(
                            "System.Process.Files",
                            now,
                            new Dictionary<string, object>
                            {
                                    { "machine", Environment.MachineName },
                                    { "processId", pid },
                                    { "executable", procName },
                                    { "file", fileName },
                                    { "deleted", isDeleted },
                            }));
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "Failed to log info for process {id}", pid);
                }
            }
        }
    }
}
