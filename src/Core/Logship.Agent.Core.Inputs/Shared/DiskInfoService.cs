using Logship.Agent.Core.Events;
using Logship.Agent.Core.Records;
using Logship.Agent.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Logship.Agent.Core.Inputs.Shared
{
    /// <summary>
    /// The disk info service emits disk statistics.
    /// </summary>
    internal class DiskInfoService : BaseConfiguredService
    {
        private readonly IEventBuffer eventBuffer;

        private TimeSpan interval = TimeSpan.FromSeconds(5);

        public DiskInfoService(IEventBuffer eventBuffer, ILogger logger)
            : base(nameof(DiskInfoService), logger)
        {
            this.eventBuffer = eventBuffer;
        }

        public override void UpdateConfiguration(IConfigurationSection configuration)
        {
            // Just pull that interval.
            this.interval = configuration.GetTimeSpan(nameof(this.interval), this.interval, this.Logger);
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            do
            {
                var drives = DriveInfo.GetDrives();

                var now = DateTimeOffset.UtcNow;
                foreach (var drive in drives)
                {
                    this.Logger.LogTrace("Found drive: {drive} {type}", drive.Name, drive.DriveType);
                    var record = new DataRecord(
                        "filesystem.drives",
                        now,
                        new Dictionary<string, object>
                        {
                            { "name", drive.Name },
                            { "machine", Environment.MachineName },
                            { "type", drive.DriveType.ToString() },
                            { "root_dir", drive.RootDirectory.FullName },
                        });
                    try
                    {
                        record.Data.Add("total_size_bytes", drive.TotalSize);
                        record.Data.Add("total_freespace_bytes", drive.TotalFreeSpace);
                        record.Data.Add("available_freespace_bytes", drive.AvailableFreeSpace);
                        record.Data.Add("volume_label", drive.VolumeLabel);
                        record.Data.Add("format", drive.DriveFormat);
                    } catch (IOException ex)
                    {
                        this.Logger.LogError("Error reading drive {drive}. {exception}", drive.Name, ex);
                    }
                    this.eventBuffer.Add(record);
                }

                await Task.Delay(this.interval, token);
            } while (false == token.IsCancellationRequested);
        }
    }
}
