namespace Logship.Agent.Core.Records
{
    public sealed record DataRecord(string Schema, DateTimeOffset TimeStamp, Dictionary<string, object> Data)
    {
    }
}
