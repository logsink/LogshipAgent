using Logship.Agent.Service.Configuration;
using Logship.Agent.Service.Internals;
using Logship.Agent.Service.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Text.Json;

using var tokenSource = new CancellationTokenSource();
Console.CancelKeyPress += Console_CancelKeyPress;

void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    tokenSource.Cancel();
    e.Cancel = true;
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

var consoleProvider = new ConsoleLoggerProvider(
        options: consoleOptions,
        formatters: null);
//consoleProvider.SetScopeProvider(new Loggerfactory());

var loggerFactory = new LoggerFactory(new ILoggerProvider[] { consoleProvider },
        new SimpleOptionsMonitor<LoggerFilterOptions>(new LoggerFilterOptions
        {
            MinLevel = LogLevel.Information,
        }),
        Options.Create(new LoggerFactoryOptions
        {
            ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.Tags
        }));


var configuration = JsonSerializer.Deserialize<RootConfiguration>(File.ReadAllText("appsettings.json"), new SourceGenerationContext(new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
}).RootConfiguration);

if (configuration == null)
{
    // If the configuration is null, then deserialization failed.
    // Just log an error and exit.
    loggerFactory.CreateLogger("root").LogError("Failed to deserialize configuration.");
    return;
}

var host = new RootServiceHost(configuration, loggerFactory.CreateLogger("root"));

await host.StartAsync(tokenSource.Token);

try
{
    await Task.Delay(-1, tokenSource.Token);
}
catch (OperationCanceledException)
{
}

using (var shutdownToken = new CancellationTokenSource())
{
    shutdownToken.CancelAfter(5000);
    await host.StopAsync(shutdownToken.Token);
}