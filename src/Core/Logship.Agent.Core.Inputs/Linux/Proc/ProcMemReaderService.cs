using Logship.Agent.Core.Events;
using Logship.Agent.Core.Inputs.Linux.JournalCtl;
using Logship.Agent.Core.Records;
using Logship.Agent.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Logship.Agent.Core.Inputs.Linux.Proc
{
    internal class ProcMemReaderService : BaseConfiguredService
    {
        private readonly IEventBuffer eventSink;
        private readonly Dictionary<int, ProcPidData> processes;
        private TimeSpan interval = TimeSpan.FromSeconds(5);

        public ProcMemReaderService(IEventBuffer buffer, ILogger logger) : base(nameof(JournalCtlService), logger)
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
                this.Logger.LogWarning($"Invalid configuration to execute {nameof(ProcMemReaderService)} in a non-Linux environment.");
                return;
            }

            var clockTicks = int.Parse(await this.ExecuteLinuxCommand("getconf", "CLK_TCK", token));

            while (false == token.IsCancellationRequested)
            {
                this.ReadProcStat();
                this.ReadPerProcessStat(clockTicks);
                await Task.Delay(this.interval, token);
            }
        }

        private void ReadPerProcessStat(int tickRequency)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var processDirectory in Directory.EnumerateDirectories("/proc"))
            {
                if (false == int.TryParse(Path.GetFileName(processDirectory), out var pid))
                {
                    continue;
                }

                this.Logger.LogTrace("Logging for process {id} {dir}", processDirectory, Path.GetFileName(processDirectory));

                try
                {
                    var procName = File.ReadAllText(Path.Combine(processDirectory, "cmdline")).Split(" ")[0];

                    var line = File.ReadAllText(Path.Combine(processDirectory, "stat"));
                    var split = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                    var userTime = int.Parse(split[13]);
                    var kernalTIme = int.Parse(split[14]);

                    var numThreads = int.Parse(split[19]);

                    var rssSizePages = long.Parse(split[23]);

                    this.eventSink.Add(new DataRecord(
                            "System.Process.Threads",
                            now,
                            new Dictionary<string, object>
                            {
                                    { "machine", Environment.MachineName },
                                    { "processId", pid },
                                    { "executable", procName },
                                    { "count", numThreads }
                            }));

                    this.eventSink.Add(new DataRecord(
                            "System.Process.Memory",
                            now,
                            new Dictionary<string, object>
                            {
                                    { "machine", Environment.MachineName },
                                    { "processId", pid },
                                    { "executable", procName },
                                    { "bytes", rssSizePages * 4 * 1024 } // Convert to bytes.
                            }));

                    if (false == this.processes.TryGetValue(pid, out var data))
                    {
                        data = new ProcPidData(Stopwatch.StartNew(), procName, pid, userTime, kernalTIme, numThreads);
                        this.processes.Add(pid, data);
                    }
                    else
                    {
                        var timeDiff = data.watch.Elapsed;
                        var totalTicks = timeDiff.TotalSeconds * tickRequency;
                        var percentCpu = (double)((userTime + kernalTIme) - data.TotalTicks) / (double)totalTicks * 100.0;

                        this.eventSink.Add(new DataRecord(
                            "System.Process.Cpu",
                            now,
                            new Dictionary<string, object>
                            {
                                    { "machine", Environment.MachineName },
                                    { "processId", pid },
                                    { "executable", procName },
                                    { "percentage", percentCpu }
                            }));

                        this.processes[pid] = data with
                        {
                            watch = Stopwatch.StartNew(),
                            kernalTime = kernalTIme,
                            userTime = userTime,
                            numThreads = numThreads,
                        };
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.LogError("Failed to read /proc/{pid}/stat {Ex}", pid, ex); 
                }
            }
        }

        private void ReadProcStat()
        {
            try
            {
                var now = DateTime.UtcNow;
                var recordLinux = new DataRecord(
                    "Linux.Proc.Stat",
                    now,
                    new Dictionary<string, object>
                    {
                            { "machine", Environment.MachineName },
                    });

                var recordSystem = new DataRecord(
                    "System.Memory",
                    now,
                    new Dictionary<string, object>
                    {
                            { "machine", Environment.MachineName },
                    });

                using (var readerStream = File.OpenRead("/proc/meminfo"))
                using (var reader = new StreamReader(readerStream))
                {
                    while (true)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrEmpty(line))
                        {
                            break;
                        }
                        var split = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        var name = split[0].Trim(':');
                        var size = long.Parse(split[1]);
                        var multiplier = 1L;
                        if (split.Length > 2)
                        {
                            var sizeStr = split[2];
                            switch (sizeStr)
                            {
                                case "kB":
                                    multiplier = 1024L;
                                    break;
                                case "mB":
                                    multiplier = 1024L * 1024;
                                    break;
                                case "gB":
                                    multiplier = 1024L * 1024 * 1024;
                                    break;
                                default:
                                    this.Logger.LogError("Unknown multiplier: {multiplier}", sizeStr);
                                    break;
                            }
                        }

                        size *= multiplier;
                        recordLinux.Data.Add(name.Replace("(", "_").Replace(")", "_"), size);

                        switch (name)
                        {
                            case "MemTotal":
                                recordSystem.Data.Add("TotalMemory", size);
                                break;
                            case "MemFree":
                                recordSystem.Data.Add("FreeMemory", size);
                                break;
                        }
                    }

                }

                this.eventSink.Add(recordSystem);
                this.eventSink.Add(recordLinux);
            }
            catch (Exception ex)
            {
                this.Logger.LogError("Failed to read /proc/stat {Ex}", ex);
            }
        }

        private async Task<string> ExecuteLinuxCommand(string command, string args, CancellationToken token)
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            })!;
            await process.WaitForExitAsync(token);
            return process.StandardOutput.ReadToEnd();
        }
    }
}
