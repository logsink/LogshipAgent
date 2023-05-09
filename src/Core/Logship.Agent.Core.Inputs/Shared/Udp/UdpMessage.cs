using Logship.Agent.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Logship.Agent.Core.Inputs.Shared.Udp
{
    public record UdpMessage(DateTimeOffset? Timestamp, string Schema, Dictionary<string, object> Data)
    {
    }

    [JsonSerializable(typeof(UdpMessage))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(ulong))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(uint))]
    [JsonSerializable(typeof(float))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(Guid))]
    [JsonSerializable(typeof(TimeSpan))]
    [JsonSerializable(typeof(DateTime))]
    [JsonSerializable(typeof(DateTimeOffset))]
    [JsonSerializable(typeof(bool))]
    public partial class UdpMessageSerializationContext : JsonSerializerContext
    {
    }
}
