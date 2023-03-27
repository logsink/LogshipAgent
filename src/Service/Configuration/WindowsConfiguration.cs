namespace Logship.Agent.Service.Configuration
{
    internal class WindowsConfiguration : BaseServiceConfiguration
    {
        public WindowsConfiguration()
        {
            this.Enabled = OperatingSystem.IsWindows();
        }

        public WindowsPerformanceCounterConfiguration PerformanceCounter { get; set; } = new WindowsPerformanceCounterConfiguration();
    }
}
