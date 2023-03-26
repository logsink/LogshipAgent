using System.Text.Json.Serialization;

namespace Logship.Agent.Service.Records
{
    [JsonSerializable(typeof(DataRecord))]
    [JsonSerializable(typeof(IEnumerable<DataRecord>))]
    internal partial class RecordSourceGenerationContext : JsonSerializerContext
    {
    }
}
