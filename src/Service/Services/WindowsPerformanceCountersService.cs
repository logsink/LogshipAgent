
using Logship.Agent.Service;
using Logship.Agent.Service.Collectors;
using Logship.Agent.Service.Configuration;
using Logship.Agent.Service.Records;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Enumeration;
using System.Text;

/// <summary>
/// Fetches values for windows performance counters.
/// </summary>
internal sealed class WindowsPerformanceCountersService : BaseAsyncService
{
    private readonly WindowsPerformanceCounterConfiguration configuration;
    private readonly InfoSink sink;

    public WindowsPerformanceCountersService(WindowsPerformanceCounterConfiguration configuration, InfoSink sink, ILogger logger) : base("PerformaceCounterService", logger)
    {
        this.configuration = configuration;
        this.sink= sink;
    }

    protected override Task OnStart(CancellationToken token)
    {
        return base.OnStart(token);
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        var counters = this.GetUniqueCountersForQueries(this.configuration.Counters);
        var counterRefresh = Stopwatch.StartNew();

        this.Logger.LogInformation("Fetched {0} counters. Beginning counter uploads.", counters.Count);

        while (false == token.IsCancellationRequested)
        {
            // Check if we need to refresh out counters.
            if (counterRefresh.Elapsed > this.configuration.CounterRefreshInterval)
            {
                counters = this.GetUniqueCountersForQueries(this.configuration.Counters);
                counterRefresh.Restart();
            }

            // Read each counter, and push the data into the info sink.
            foreach (var counter in counters)
            {
                this.sink.Push(new DataRecord(counter.Key.GetName(), DateTimeOffset.UtcNow, new Dictionary<string, object>
                {
                    { "machine", Environment.MachineName },
                    { "catagory", counter.Key.Catagory },
                    { "counter", counter.Key.CounterName },
                    { "instance", counter.Key.InstanceName },
                    { "value", counter.Value.RawValue.ToString() }
                }));
            }

            await Task.Delay(this.configuration.Interval);
        }
    }

    private record CounterSearchEntry(string Catagory, string Name, string Instance)
    {
        public static CounterSearchEntry FromString(string input)
        {
            var match = WindowsPerformanceCounterConfiguration.PerformanceCounterRegex().Match(input);
            if (false == match.Success)
            {
                throw new ArgumentException("Invalid input", nameof(input));
            }
            return new CounterSearchEntry(match.Groups["catagory"].Value, match.Groups["counter"].Value, match.Groups["instance"].Value);
        }
    }

    private record CounterEntryKey(string Catagory, string CounterName, string InstanceName)
    {
        private readonly string name = SanitizeName($"windows.{Catagory}.{CounterName}");

        public string GetName()
        {
            return this.name;
        }

        private static string SanitizeName(string name)
        {
            var result = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (char.IsLetter(c) || char.IsNumber(c) || c == '.' || c == '-' || c == '_')
                {
                    result.Append(c);
                }
                else
                {
                    result.Append('_');
                }
            }
            return result.ToString();
        }
    
    }

    private IDictionary<CounterEntryKey, PerformanceCounter> GetUniqueCountersForQueries(IReadOnlyList<string> paths)
    {
        if (OperatingSystem.IsWindows())
        {
            var results = new Dictionary<CounterEntryKey, PerformanceCounter>();

            var counterSearches = paths.Select(CounterSearchEntry.FromString).ToList();

            foreach (var counterSearch in counterSearches)
            {
                this.Logger.LogInformation("Searching for counter: \\{0}({1})\\{2}", counterSearch.Catagory, counterSearch.Name, counterSearch.Instance);

                if (false == PerformanceCounterCategory.Exists(counterSearch.Catagory))
                {
                    this.Logger.LogWarning("Counter catagory {0} does not exist", counterSearch.Catagory);
                    continue;
                }

                var category = new PerformanceCounterCategory(counterSearch.Catagory);

                var instances = category.GetInstanceNames()
                    .Where(instance => FileSystemName.MatchesSimpleExpression(counterSearch.Instance, instance, true))
                    .ToList();

                if (false == instances.Any())
                {
                    this.Logger.LogWarning("Counter: \\{0}({1})\\{2} matched no instances", counterSearch.Catagory, counterSearch.Name, counterSearch.Instance);
                    continue;
                }

                foreach (var instance in instances.SelectMany(i => category.GetCounters(i).Where(c => FileSystemName.MatchesSimpleExpression(counterSearch.Name, c.CounterName))))
                {
                    this.Logger.LogDebug("Found counter: \\{0}({1})\\{2} for query: \\{3}({4})\\{5}", instance.CategoryName, instance.CounterName, instance.InstanceName, counterSearch.Catagory, counterSearch.Name, counterSearch.Instance);
                    results[new CounterEntryKey(instance.CategoryName, instance.CounterName, instance.InstanceName)] = instance;
                }
            }

            return results;
        }

        throw new NotImplementedException();
    }
}