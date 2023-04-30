using Logship.Agent.Core.Events;
using Logship.Agent.Core.Services;
using Microsoft.Extensions.Logging;

namespace Logship.Agent.Core.Inputs.Shared
{
    internal class SysInfoService : BaseInputService
    {
        public SysInfoService(IEventBuffer buffer, ILogger logger) : base(buffer, nameof(SysInfoService), logger)
        {

        }

        protected override Task ExecuteSingleAsync(CancellationToken token)
        {
            var c = CreateRecord("System.Environment");
            c.Data[nameof(Environment.OSVersion.Platform)] = Environment.OSVersion.Platform;
            c.Data[nameof(Environment.OSVersion.Version)] = Environment.OSVersion.Version;
            c.Data[nameof(Environment.OSVersion.VersionString)] = Environment.OSVersion.VersionString;
            c.Data[nameof(Environment.MachineName)] = Environment.MachineName;
            c.Data[nameof(Environment.ProcessorCount)] = Environment.ProcessorCount;
            c.Data[nameof(Environment.CommandLine)] = Environment.CommandLine;
            c.Data[nameof(Environment.TickCount64)] = Environment.TickCount64;
            c.Data[nameof(Environment.CurrentDirectory)] = Environment.CurrentDirectory;
            c.Data[nameof(Environment.StackTrace)] = Environment.StackTrace;
            c.Data[nameof(Environment.Version)] = Environment.Version;
            c.Data[nameof(Environment.UserDomainName)] = Environment.UserDomainName;
            c.Data[nameof(Environment.UserInteractive)] = Environment.UserInteractive;
            c.Data[nameof(Environment.UserName)] = Environment.UserName;
            c.Data[nameof(Environment.CurrentManagedThreadId)] = Environment.CurrentManagedThreadId;
            c.Data[nameof(Environment.ProcessId)] = Environment.ProcessId;
            c.Data[nameof(Environment.Is64BitOperatingSystem)] = Environment.Is64BitOperatingSystem;
            c.Data[nameof(Environment.Is64BitProcess)] = Environment.Is64BitProcess;
            c.Data[nameof(Environment.ProcessPath)] = Environment.ProcessPath ?? string.Empty;
            c.Data[nameof(Environment.SystemDirectory)] = Environment.SystemDirectory;
            c.Data[nameof(Environment.WorkingSet)] = Environment.WorkingSet;
            c.Data[nameof(Environment.SystemPageSize)] = Environment.SystemPageSize;
            this.Buffer.Add(c);
            return Task.CompletedTask;
        }
    }
}
