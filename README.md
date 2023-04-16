# Introduction 
TODO: Give a short introduction of your project. Let this section explain the objectives or the motivation behind this project. 

# Configuration

## Output

The Output section specifies where the metrics data should be sent.
- `interval`: The interval at which performance counter metrics are output.
- `endpoint`: The logship upload endpoint, or `"console"`.

## Logging
The Logging section specifies the logging configuration for the metrics collection process.

## Inputs
The Inputs section specifies the different sources of metrics data that will be collected. All inputs have a required `type` configuration, which allows you to specify the input type. 

* HealthService: Tracks agent health.
    - `interval`: The interval at which Agent health metrics are measured.
* Windows.PerformanceCounters: Tracks windows performance counter metrics.
    - `interval`: The interval at which performance counter metrics are measured.
    - `counters`: An array of counters to search and upload.
* Windows.Etw: This input collects data from Event Tracing for Windows (ETW) providers.
    - `providers`: An array of ETW provider configuration.
        - `ProviderName`: The name of an ETW provider to collect (optional if `ProviderGuid` is specified)
        - `ProviderGuid`: The GUID of an ETW provider to collect (optional if `ProviderName` is specified)
        - `Keywords`: ETW TraceEventKeyword to filter. (optional)
        - `Level`: ETW [TraceEventLevel](https://referencesource.microsoft.com/#System.ServiceModel.Internals/System/Runtime/TraceEventLevel.cs) to collect (optional, defaults to ALL)

Overall, this configuration file specifies the sources of metrics data to be collected and how frequently the data should be collected and outputted. This file can be used by a metrics collection tool to collect and monitor metrics data from various sources.

# Getting Started
TODO: Guide users through getting your code up and running on their own system. In this section you can talk about:
1.	Installation process
2.	Software dependencies
3.	Latest releases
4.	API references

# Build and Test
TODO: Describe and show how to build your code and run the tests. 

# Contribute
TODO: Explain how other users and developers can contribute to make your code better. 

If you want to learn more about creating good readme files then refer the following [guidelines](https://docs.microsoft.com/en-us/azure/devops/repos/git/create-a-readme?view=azure-devops). You can also seek inspiration from the below readme files:
- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)
- [Chakra Core](https://github.com/Microsoft/ChakraCore)