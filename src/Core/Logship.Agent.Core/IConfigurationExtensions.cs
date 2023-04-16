using Logship.Agent.Core.Configuration;
using Logship.Agent.Core.Internals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logship.Agent.Core
{
    public static class IConfigurationExtensions
    {
        public static T GetValue<T>(this IConfiguration configuration, string propertyName, Func<string, T?> factory, ILogger logger)
        {
            string configPath = string.Empty;
            if (configuration is IConfigurationSection section)
            {
                configPath = section.Path;
            }

            string? value = configuration[propertyName];
            if (value == null)
            {
                logger.LogError("Null configuration value at {path}:{configProperty}", configPath, propertyName);
                throw new ConfigurationException($"Null configuration at path {configPath}. Expecting type {typeof(T).FullName}");
            }

            T? val = factory(value);
            if (val == null)
            {
                logger.LogError("Failed to convert configuration value at {path}:{configProperty}", configPath, propertyName);
                throw new ConfigurationException($"Failed to convert configuration at path {configPath}. Expecting type {typeof(T).FullName}");
            }

            logger.LogInformation("Loaded configuration value {path}:{configProperty} = {value}", configPath, propertyName, val);
            return val;
        }

        public static T? GetValueOrDefault<T>(this IConfiguration configuration, string propertyName, Func<string, T?> factory, ILogger logger)
        {
            string configPath = string.Empty;
            if (configuration is IConfigurationSection section)
            {
                configPath = section.Path;
            }

            string? value = configuration[propertyName];
            if (value == null)
            {
                logger.LogInformation("Failed to load configuration value {path}:{configProperty}. Using default.", configPath, propertyName);
                return default;
            }

            T? temp = factory(value);
            if (temp == null)
            {
                logger.LogInformation("Failed to convert configuration value {path}:{configProperty} = {value}. Using default.", configPath, propertyName, value);
                return default;
            }

            logger.LogInformation("Loaded configuration value {path}:{configProperty} = {value}", configPath, propertyName, temp);
            return temp;
        }

        public static IReadOnlyList<T> GetValues<T>(this IConfiguration configuration, string propertyName, Func<string?, T?> factory, ILogger logger)
        {
            string configPath = string.Empty;
            if (configuration is IConfigurationSection section)
            {
                configPath = section.Path;
            }

            var children = configuration.GetSection(propertyName)
                .GetChildren()
                .ToList();

            var result = new List<T>(children.Count);

            int index = 0;
            foreach (var child in children)
            {
                T? val = factory(child.Value);
                if (val == null)
                {
                    logger.LogError("Failed to convert configuration value at {path}:{configProperty}[{index}]", configPath, propertyName, index);
                    throw new ConfigurationException($"Failed to convert configuration at path {configPath}:{propertyName}[{index}]. Expecting type {typeof(T).FullName}");
                }

                result.Add(val);
                index++;
            }

            return result;
        }

        public static string GetRequiredStringValue(this IConfiguration config, string propertyName, ILogger logger)
        {
            return config.GetValue(propertyName, str => str, logger);
        }

        public static TimeSpan GetRequiredTimeSpanValue(this IConfiguration config, string propertyName, ILogger logger)
        {
            return config.GetValue(propertyName, str =>
            {
                return TimeSpan.TryParse(str, out var i)
                    ? i :
                    throw new ConfigurationException($"Configuration was not a valid TimeSpan.");
            }, logger);
        }

        public static TimeSpan GetTimeSpanValue(this IConfiguration config, string propertyName, TimeSpan defaultValue, ILogger logger)
        {
            return config.GetValueOrDefault(propertyName, str =>
            {
                if (TimeSpan.TryParse(str, out var i))
                {
                    return i;
                }

                return defaultValue;
            }, logger);
        }

        public static IReadOnlyList<string> GetValues(this IConfiguration config, string propertyName, ILogger logger)
        {
            return config.GetValues(propertyName, str => str, logger);
        }
    }
}
