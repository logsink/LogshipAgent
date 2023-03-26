namespace Logship.Agent.Service.Records
{
    internal record DataRecord(string Schema, DateTimeOffset TimeStamp, Dictionary<string, object> Data)
    {
    }
}
