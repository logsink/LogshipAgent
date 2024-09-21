using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logship.Agent.Core.Services.Sources.Common.Otlp
{
    internal sealed class ExtractResourceAttribute
    {
        public ExtractResourceAttribute(string key, object defaultValue)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(key, nameof(key));
            ArgumentNullException.ThrowIfNull(defaultValue, nameof(defaultValue));
            Key = key;
            DefaultValue = defaultValue;
        }

        public readonly string Key;
        public readonly object DefaultValue;
    }
}
