using System.ComponentModel.DataAnnotations;

namespace Logship.Agent.Service.Configuration
{
    internal class PushConfiguration
    {
        public string? Endpoint { get; set; }

        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(5);
    }
}
