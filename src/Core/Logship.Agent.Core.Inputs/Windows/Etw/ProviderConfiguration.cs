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
                providerName: configuration.GetString(nameof(ProviderName), string.Empty, logger)!,
                level: configuration.GetEnum(nameof(Level), TraceEventLevel.Always, logger),
                keywords: configuration.GetEnum(nameof(Keywords), TraceEventKeyword.All, logger),
                providerGuid: configuration.GetValue(nameof(ProviderGuid), str =>
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
