using System.Text.Json.Serialization;

namespace Logship.Agent.Core.Internals.Models
{
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(AgentRefreshResponseModel))]
    [JsonSerializable(typeof(AgentRegistrationRequestModel))]
    [JsonSerializable(typeof(AgentRegistrationResponseModel))]
    internal sealed partial class ModelSourceGenerationContext : JsonSerializerContext
    {
    }
}
