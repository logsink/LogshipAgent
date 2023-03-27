namespace Logship.Agent.Service.Configuration
{
    internal class RootConfiguration
    {
        public PushConfiguration PushService { get; set; } = new PushConfiguration();
        public UptimeServiceConfiguration UptimeService { get; set; } = new UptimeServiceConfiguration();

        public WindowsConfiguration Windows { get; set; } = new WindowsConfiguration();
    }
}
