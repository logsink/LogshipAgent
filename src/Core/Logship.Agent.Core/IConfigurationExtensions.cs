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
        public static T GetRequiredValue<T>(this IConfiguration configuration, string propertyName, Func<string, T> factory, ILogger logger)
        {
            string configPath = string.Empty;
            if (configuration is IConfigurationSection section)
            {
                configPath = section.Path;
            }

            string? value = configuration[propertyName];
            if (value == null)
            {
                logger.LogError("Null configuration for required value at {path}:{property}", configPath, propertyName);
                throw new ConfigurationRequiredException($"Null configuration at path {configPath}:{propertyName}.");
            }

            try
            {
                T val = factory(value);
                logger.LogInformation("Loaded configuration value at {path}:{property}. Value = {value}. Raw = {input}.", configPath, propertyName, val, value);
                return val;
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load configuration value at {path}:{property}. {exception}", configPath, propertyName, ex);
                throw new ConfigurationException("Failed to load required configuration value.", ex);
            }
        }

        public static T GetValue<T>(this IConfiguration configuration, string propertyName, Func<string?, T> factory, ILogger logger)
        {
            string configPath = string.Empty;
            if (configuration is IConfigurationSection section)
            {
                configPath = section.Path;
            }

            string? value = configuration[propertyName];
            try
            {
                T val = factory(value);
                logger.LogInformation("Loaded configuration value at {path}:{property}. Value = {value}. Raw = {input}.", configPath, propertyName, val, value);
                return val;
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load configuration value at {path}:{property}. {exception}", configPath, propertyName, ex);
                throw new ConfigurationException("Failed to load configuration value.", ex);
            }
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

        public static Guid GetRequiredGuid(this IConfiguration config, string propertyName, ILogger logger)
        {
            return config.GetRequiredValue(propertyName, str => Guid.Parse(str), logger);
        }

        public static string GetRequiredString(this IConfiguration config, string propertyName, ILogger logger)
        {
            return config.GetRequiredValue(propertyName, str => str, logger);
        }

        public static string GetString(this IConfiguration config, string propertyName, string defaultValue, ILogger logger)
        {
            return config.GetValue(propertyName, str => str ?? defaultValue, logger);
        }

        public static TEnum GetRequiredEnum<TEnum>(this IConfiguration configuration, string propertyName, ILogger logger)
            where TEnum : struct, System.Enum
        {
            return configuration.GetRequiredValue(propertyName, str =>
            {
                if (Enum.TryParse<TEnum>(str, out var result))
                {
                    return result;
                }

                throw new ConfigurationFormatException($"Invalid enum format for {typeof(TEnum).FullName}");
            }, logger);
        }

        public static TEnum GetEnum<TEnum>(this IConfiguration configuration, string propertyName, TEnum defaultValue, ILogger logger)
            where TEnum : struct, System.Enum
        {
            return configuration.GetValue(propertyName, str =>
            {
                if (Enum.TryParse<TEnum>(str, out var result))
                {
                    return result;
                }

                return defaultValue;
            }, logger);
        }

        public static bool GetBool(this IConfiguration configuration, string propertyName, bool defaultValue, ILogger logger)
        {
            return configuration.GetValue(propertyName, str =>
            {
                if (bool.TryParse(str, out var t))
                {
                    return t;
                }

                return defaultValue;
            }, logger);
        }

        public static TimeSpan GetTimeSpan(this IConfiguration configuration, string propertyName, TimeSpan defaultValue, ILogger logger)
        {
            return configuration.GetValue(propertyName, str =>
            {
                if (TimeSpan.TryParse(str, out var t))
                {
                    return t;
                }

                return defaultValue;
            }, logger);
        }

        public static int GetInt(this IConfiguration configuration, string propertyName, int defaultValue, ILogger logger)
        {
            return configuration.GetValue(propertyName, str =>
            {
                if (int.TryParse(str, out var t))
                {
                    return t;
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
