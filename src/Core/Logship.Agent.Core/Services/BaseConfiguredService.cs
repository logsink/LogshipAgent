using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logship.Agent.Core.Services
{
    public abstract class BaseConfiguredService : BaseAsyncService
    {
        protected BaseConfiguredService(string serviceName, ILogger logger)
            : base(serviceName, logger)
        {
        }

        public abstract void UpdateConfiguration(IConfigurationSection configuration);
    }
}
