using System.Text.Json.Serialization;

namespace Logship.Agent.Core.Records
{
    [JsonSerializable(typeof(DataRecord))]
    [JsonSerializable(typeof(IEnumerable<DataRecord>))]
    [JsonSerializable(typeof(IReadOnlyCollection<DataRecord>))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(ulong))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(uint))]
    [JsonSerializable(typeof(float))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(decimal))]
    [JsonSerializable(typeof(Guid))]
    [JsonSerializable(typeof(TimeSpan))]
    [JsonSerializable(typeof(DateTime))]
    [JsonSerializable(typeof(DateTimeOffset))]
    [JsonSerializable(typeof(bool))]
    internal partial class RecordSourceGenerationContext : JsonSerializerContext
    {
    }
}
