using Microsoft.Extensions.Options;

namespace Logship.Agent.Core.Configuration.Validators
{
    [OptionsValidator]
    public sealed partial class SourcesConfigurationValidator : IValidateOptions<SourcesConfiguration>
    {
    }
}
