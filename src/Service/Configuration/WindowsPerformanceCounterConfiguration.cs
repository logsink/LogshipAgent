using System.Text.RegularExpressions;

namespace Logship.Agent.Service.Configuration
{
    internal partial class WindowsPerformanceCounterConfiguration : BaseServiceConfiguration
    {
        [GeneratedRegex(@"\\(?<catagory>.+)\((?<counter>.+)\)\\(?<instance>.*)", RegexOptions.IgnoreCase, "en-US")]
        public static partial Regex PerformanceCounterRegex();

        public IReadOnlyList<string> Counters { get; set; } = new List<string>();

        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan CounterRefreshInterval { get; set; } = TimeSpan.FromMinutes(5);
    }
}
