using Logship.Agent.Core.Events;
using Logship.Agent.Core.Inputs.Linux.JournalCtl;
using Logship.Agent.Core.Records;
using Logship.Agent.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Logship.Agent.Core.Inputs.Linux.Proc
{
    internal class ProcMemReaderService : BaseConfiguredService
    {
        private readonly IEventBuffer eventSink;

        private TimeSpan interval = TimeSpan.FromSeconds(5);

        public ProcMemReaderService(IEventBuffer buffer, ILogger logger) : base(nameof(JournalCtlService), logger)
        {
            this.eventSink = buffer;
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

            while (false == token.IsCancellationRequested)
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
                

                await Task.Delay(this.interval, token);
            }
        }
    }
}
