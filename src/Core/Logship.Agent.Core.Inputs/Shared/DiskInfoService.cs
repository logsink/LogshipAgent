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
                    this.Logger.LogTrace("Found drive: {drive} {type} {size} {totalFree} {availableFree}", drive.Name, drive.DriveType, drive.TotalSize, drive.TotalFreeSpace, drive.AvailableFreeSpace);

                    this.eventBuffer.Add(new DataRecord(
                        "filesystem.drives",
                        now,
                        new Dictionary<string, object>
                        {
                            { "name", drive.Name },
                            { "machine", Environment.MachineName },
                            { "type", drive.DriveType.ToString() },
                            { "total_size_bytes", drive.TotalSize },
                            { "total_freespace_bytes", drive.TotalFreeSpace },
                            { "available_freespace_bytes", drive.AvailableFreeSpace },
                            { "volume_label", drive.VolumeLabel },
                            { "root_dir", drive.RootDirectory.FullName },
                            { "format", drive.DriveFormat }
                        }));
                }

                await Task.Delay(this.interval, token);
            } while (false == token.IsCancellationRequested);
        }
    }
}
