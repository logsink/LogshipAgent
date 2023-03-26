using System.Text.Json.Serialization;

namespace Logship.Agent.Service.Configuration
{
    [JsonSerializable(typeof(RootConfiguration))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
