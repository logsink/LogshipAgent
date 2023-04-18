using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logship.Agent.Core.Configuration
{
    [Serializable]
    public class ConfigurationFormatException : ConfigurationException
    {
        public ConfigurationFormatException()
        {
        }

        public ConfigurationFormatException(string? message) : base(message)
        {
        }

        public ConfigurationFormatException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
