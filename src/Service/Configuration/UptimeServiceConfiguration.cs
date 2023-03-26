namespace Logship.Agent.Service.Configuration
{
    internal class UptimeServiceConfiguration : BaseServiceConfiguration
    {
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(5);
    }
}
