namespace Logship.Agent.Core.Records
{
    public record DataRecord(string Schema, DateTimeOffset TimeStamp, Dictionary<string, object> Data)
    {
    }
}
