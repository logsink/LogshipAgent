using Microsoft.Diagnostics.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logship.Agent.Core.Inputs.Windows.Etw
{
    internal class ProviderConfiguration
    {
        public ProviderConfiguration(string providerName, TraceEventLevel level, TraceEventKeyword keywords, Guid providerGuid)
        {
            ProviderName = providerName;
            Level = level;
            Keywords = keywords;
            ProviderGuid = providerGuid;
        }

        public string ProviderName { get; set; }
        public TraceEventLevel Level { get; set; }
        public TraceEventKeyword Keywords { get; set; }
        public Guid ProviderGuid { get; set; }


        public static bool TryFrom(IConfigurationSection configuration, ILogger logger, out ProviderConfiguration result)
        {
            result = new ProviderConfiguration(
                providerName: configuration.GetValueOrDefault(nameof(ProviderName), str => str ?? string.Empty, logger)!,
                level: configuration.GetValueOrDefault(nameof(Level), str =>
                {
                    if (Enum.TryParse(str, out TraceEventLevel level))
                    {
                        return level;
                    }

                    return TraceEventLevel.Always;
                }, logger),
                keywords: configuration.GetValueOrDefault(nameof(Keywords), str =>
                {
                    if (Enum.TryParse(str, out TraceEventKeyword keyword))
                    {
                        return keyword;
                    }

                    return TraceEventKeyword.All;
                }, logger),
                providerGuid: configuration.GetValueOrDefault(nameof(ProviderGuid), str =>
                {
                    if (Guid.TryParse(str, out Guid guid))
                    {
                        return guid;
                    }

                    return Guid.Empty;
                }, logger));
            return !string.IsNullOrWhiteSpace(result.ProviderName) || result.ProviderGuid != Guid.Empty;
        }
    }
}
