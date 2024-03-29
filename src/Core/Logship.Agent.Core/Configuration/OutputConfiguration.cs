using Logship.Agent.Core.Configuration.Validators.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Logship.Agent.Core.Configuration
{
    public sealed class OutputConfiguration
    {
        public const string CONSOLEOUTPUT = "console";

        [Required]
        [JsonPropertyName("endpoint")]
		[ConfigurationKeyName("endpoint")]
        public string Endpoint { get; set; } = CONSOLEOUTPUT;

        [Required]
        [JsonPropertyName("subscription")]
		[ConfigurationKeyName("subscription")]
        public Guid Subscription { get; set; } = Guid.Empty;

        [Required]
        [PositiveTimeSpan]
        [JsonPropertyName("interval")]
		[ConfigurationKeyName("interval")]
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(10);

        [Range(5_000, 1_000_000)]
        [JsonPropertyName("maximumBufferSize")]
		[ConfigurationKeyName("maximumBufferSize")]
        public int MaximumBufferSize { get; set; } = 10_000;

        [Range(1_000, 1_000_000)]
        [JsonPropertyName("maximumFlushSize")]
		[ConfigurationKeyName("maximumFlushSize")]
        public int MaximumFlushSize { get; set; } = 10_000;

        [ValidateObjectMembers]
        [JsonPropertyName("health")]
		[ConfigurationKeyName("health")]
        public Health? Health { get; set; }
    }

    public class Health
    {
        [Required]
        [PositiveTimeSpan]
        [JsonPropertyName("interval")]
		[ConfigurationKeyName("interval")]
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(15);
    }
}
