using Logship.Agent.Core.Inputs.Linux.JournalCtl;
using Logship.Agent.Core.Inputs.Shared;
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
            return factory;
        }
    }
}