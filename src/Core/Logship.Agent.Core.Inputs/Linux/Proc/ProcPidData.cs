using System.Diagnostics;

namespace Logship.Agent.Core.Inputs.Linux.Proc
{
    internal record ProcPidData(Stopwatch watch, string filename, int processId, int userTime, int kernalTime, int numThreads)
    {
        public int TotalTicks => this.userTime + this.kernalTime;
    }
}
