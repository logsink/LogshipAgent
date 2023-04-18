
using Logship.Agent.Core.Services;
using Logship.Agent.Core;
using Logship.Agent.Core.Records;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Enumeration;
using System.Text;
using Logship.Agent.Core.Events;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

/// <summary>
/// Fetches values for windows performance counters.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Platform specific - Windows")]
internal partial class PerformanceCountersService : BaseConfiguredService
{
    private readonly IEventBuffer sink;

    [GeneratedRegex(@"\\(?<catagory>.+)\((?<counter>.+)\)\\(?<instance>.*)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex PerformanceCounterRegex();

    private TimeSpan interval;
    private TimeSpan counterRefreshInterval;
    private IReadOnlyList<string> counters;

    public PerformanceCountersService(IEventBuffer sink, ILogger logger)
        : base(nameof(PerformanceCountersService), logger)
    {
        this.sink = sink;
        this.counters = new List<string>();
    }

    protected override Task OnStart(CancellationToken token)
    {
        return base.OnStart(token);
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        if (false == OperatingSystem.IsWindows())
        {
            this.Logger.LogWarning($"Invalid configuration to execute {nameof(PerformanceCountersService)} in a non-Windows environment.");
            return;
        }

        var counters = this.GetUniqueCountersForQueries(this.counters);
        var counterRefresh = Stopwatch.StartNew();

        this.Logger.LogInformation("Fetched {count} counters. Beginning counter uploads.", counters.Count);

        while (false == token.IsCancellationRequested)
        {
            // Check if we need to refresh our counters.
            if (counterRefresh.Elapsed > this.counterRefreshInterval)
            {
                counters = this.GetUniqueCountersForQueries(this.counters);
                counterRefresh.Restart();
            }

            // Read each counter, and push the data into the info sink.
            foreach (var counter in counters)
            {
                using var p = counter.Value;
                this.sink.Add(new DataRecord(counter.Key.GetName(), DateTimeOffset.UtcNow, new Dictionary<string, object>
                {
                    { "machine", Environment.MachineName },
                    { "category", counter.Key.Category },
                    { "counter", counter.Key.CounterName },
                    { "instance", counter.Key.InstanceName },
                    { "value", p.RawValue.ToString() }
                }));

            }

            await Task.Delay(this.interval, token);
        }
    }

    private record CounterSearchEntry(string Category, string Name, string Instance)
    {
        public static CounterSearchEntry FromString(string input)
        {
            var match = PerformanceCounterRegex().Match(input);
            if (false == match.Success)
            {
                throw new ArgumentException("Invalid input", nameof(input));
            }
            return new CounterSearchEntry(match.Groups["catagory"].Value, match.Groups["counter"].Value, match.Groups["instance"].Value);
        }
    }

    private record CounterEntryKey(string Category, string CounterName, string InstanceName)
    {
        private readonly string name = SanitizeName($"windows.{Category}.{CounterName}");

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
        var results = new Dictionary<CounterEntryKey, PerformanceCounter>();
        var counterSearches = paths.Select(CounterSearchEntry.FromString).ToList();
        foreach (var counterSearch in counterSearches)
        {
            this.Logger.LogInformation("Searching for counter: \\{searchCategory}({searchName})\\{searchInstance}", counterSearch.Category, counterSearch.Name, counterSearch.Instance);
            if (false == PerformanceCounterCategory.Exists(counterSearch.Category))
            {
                this.Logger.LogWarning("Counter category {searchCategory} does not exist", counterSearch.Category);
                continue;
            }

            var category = new PerformanceCounterCategory(counterSearch.Category);

            var instances = category.GetInstanceNames()
                .Where(instance => FileSystemName.MatchesSimpleExpression(counterSearch.Instance, instance, true))
                .ToList();

            if (false == instances.Any())
            {
                this.Logger.LogWarning("Counter: \\{searchCategory}({searchName})\\{searchInstance} matched no instances", counterSearch.Category, counterSearch.Name, counterSearch.Instance);
                continue;
            }

            foreach (var instance in instances.SelectMany(i => category.GetCounters(i).Where(c => FileSystemName.MatchesSimpleExpression(counterSearch.Name, c.CounterName))))
            {
                this.Logger.LogDebug("Found counter: \\{instanceCategory}({instanceCounter})\\{instanceName} for query: \\{searchCategory}({searchName})\\{searchInstance}", instance.CategoryName, instance.CounterName, instance.InstanceName, counterSearch.Category, counterSearch.Name, counterSearch.Instance);
                results[new CounterEntryKey(instance.CategoryName, instance.CounterName, instance.InstanceName)] = instance;
            }
        }

        return results;
    }

    public override void UpdateConfiguration(IConfigurationSection configuration)
    {
        this.interval = configuration.GetTimeSpan(nameof(interval), TimeSpan.FromSeconds(5), this.Logger);
        this.counterRefreshInterval = configuration.GetTimeSpan(nameof(counterRefreshInterval), TimeSpan.FromMinutes(5), this.Logger);
        this.counters = configuration.GetValues(nameof(counters), this.Logger);
    }
}