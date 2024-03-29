using System.Text.Json.Serialization;

namespace Logship.Agent.Core.Configuration
{
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(SourcesConfiguration))]
    [JsonSerializable(typeof(OutputConfiguration))]
    internal sealed partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
