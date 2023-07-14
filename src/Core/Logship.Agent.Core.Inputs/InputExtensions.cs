using Logship.Agent.Core.Inputs.Linux.JournalCtl;
using Logship.Agent.Core.Inputs.Linux.Proc;
using Logship.Agent.Core.Inputs.Shared;
using Logship.Agent.Core.Inputs.Shared.Udp;
using Logship.Agent.Core.Inputs.Windows.Etw;

namespace Logship.Agent.Core.Inputs
{
    public static class InputExtensions
    {
        public static AgentRuntimeFactory RegisterInputs(this AgentRuntimeFactory factory)
        {
            factory.RegisterInputService("windows.performanceCounters", (b, l) => new PerformanceCountersService(b, l));
            factory.RegisterInputService("windows.etw", (b, l) => new EtwService(b, l));
            factory.RegisterInputService("journalctl", (b, l) => new JournalCtlService(b, l));
            factory.RegisterInputService("filesystem", (b, l) => new DiskInfoService(b, l));
            factory.RegisterInputService("system", (b, l) => new SysInfoService(b, l));
            factory.RegisterInputService("network", (b, l) => new NetworkInfoService(b, l));
            factory.RegisterInputService("proc", (b, l) => new ProcMemReaderService(b, l));
            factory.RegisterInputService("proc.openfiles", (b, I) => new ProcFileReaderService(b, I));
            factory.RegisterInputService("udp", (b, l) => new UdpListenerService(b, l));
            factory.RegisterInputService("processes", (b, l) => new ProcessInfoService(b, l));
            return factory;
        }
    }
}