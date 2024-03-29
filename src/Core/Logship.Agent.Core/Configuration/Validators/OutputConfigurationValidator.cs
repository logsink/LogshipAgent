using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logship.Agent.Core.Configuration.Validators
{
    [OptionsValidator]
    public sealed partial class OutputConfigurationValidator : IValidateOptions<OutputConfiguration>
    {
    }
}
