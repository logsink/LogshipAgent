using System.Text.Json.Serialization;

namespace Logship.Agent.Service.Records
{
    [JsonSerializable(typeof(DataRecord))]
    [JsonSerializable(typeof(IEnumerable<DataRecord>))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(ulong))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(uint))]
    [JsonSerializable(typeof(float))]
    [JsonSerializable(typeof(double))]
    internal partial class RecordSourceGenerationContext : JsonSerializerContext
    {
    }
}
