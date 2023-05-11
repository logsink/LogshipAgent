using Logship.Agent.ConsoleHost;
using Logship.Agent.Core;
using Logship.Agent.Core.Inputs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

using var tokenSource = new CancellationTokenSource();
Console.CancelKeyPress += Console_CancelKeyPress;
AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;

var watch = Stopwatch.StartNew();
// var consoleProvider = new ConsoleLoggerProvider(options: consoleOptions, formatters: null);

void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    tokenSource.Cancel();
    e.Cancel = true;
}

void AppDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
{
    tokenSource.Cancel();
}

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

#pragma warning disable CS0618 // Type or member is obsolete
var consoleOptions = new SimpleOptionsMonitor<ConsoleLoggerOptions>(new ConsoleLoggerOptions
{
    //FormatterName = ConsoleFormatterNames.Systemd,
    Format = ConsoleLoggerFormat.Systemd,
    IncludeScopes = true,
    UseUtcTimestamp = true,
});
#pragma warning restore CS0618 // Type or member is obsolete
IConfigurationRoot? config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.dev.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.prod.json", optional: true, reloadOnChange: true)
    .Build();
var consoleProvider = new ConsoleLoggerProvider(options: consoleOptions, formatters: null);
using var loggerFactory = new LoggerFactory(new ILoggerProvider[] { consoleProvider },
    new SimpleOptionsMonitor<LoggerFilterOptions>(new LoggerFilterOptions
    {
        MinLevel = config.GetSection("Logging").GetSection("LogLevel").GetEnum("Default", LogLevel.Information, consoleProvider.CreateLogger("ConfigLoad")),
    }),
    Options.Create(new LoggerFactoryOptions
    {
        ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.Tags
    }));
var log = loggerFactory.CreateLogger("Logship.Agent.ConsoleHost");
if (config == null)
{
    log.LogCritical("Invalid configuration. Exiting.");
    return;
}

using var agent = new AgentRuntimeFactory(config, loggerFactory)
    .RegisterInputs()
    .Build();

try
{
    await agent.StartAsync(tokenSource.Token);
    watch.Stop();
    log.LogInformation("{timestamp} - Logship Agent started. Startup duration was {durationMs}ms", DateTimeOffset.UtcNow, watch.ElapsedMilliseconds);
    await Task.Delay(-1, tokenSource.Token);
}
catch (OperationCanceledException)
{
}

try
{
    using var shutdownToken = new CancellationTokenSource();
    shutdownToken.CancelAfter(5000);
    log.LogInformation("{timestamp} - Logship Agent stopping. Startup duration was {durationMs}ms", DateTimeOffset.UtcNow, watch.ElapsedMilliseconds);
    await agent.StopAsync(shutdownToken.Token);
    log.LogInformation("{timestamp} - Logship Agent stopped. Startup duration was {durationMs}ms", DateTimeOffset.UtcNow, watch.ElapsedMilliseconds);
}
catch (OperationCanceledException)
{
}