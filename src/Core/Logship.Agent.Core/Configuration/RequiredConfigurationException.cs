using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logship.Agent.Core.Configuration
{
    [Serializable]
    public class RequiredConfigurationException : ConfigurationException
    {
        public RequiredConfigurationException()
        {
        }

        public RequiredConfigurationException(string? message) : base(message)
        {
        }

        public RequiredConfigurationException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
