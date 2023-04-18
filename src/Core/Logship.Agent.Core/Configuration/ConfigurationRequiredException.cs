using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logship.Agent.Core.Configuration
{
    [Serializable]
    public class ConfigurationRequiredException : ConfigurationException
    {
        public ConfigurationRequiredException()
        {
        }

        public ConfigurationRequiredException(string? message) : base(message)
        {
        }

        public ConfigurationRequiredException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
