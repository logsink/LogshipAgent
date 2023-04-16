using Logship.Agent.Core.Events;
using Logship.Agent.Core.Records;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Logship.Agent.Core
{
    internal class ConsoleEventOutput : IEventOutput
    {
        private readonly ILogger logger;

        public ConsoleEventOutput(ILogger logger)
        {
            this.logger = logger;
        }

        public Task<bool> SendAsync(IReadOnlyCollection<DataRecord> records, CancellationToken cancellationToken)
        {
            var groups = records.GroupBy(_ => _.Schema);
            foreach (var group in groups)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogWarning("Console output cancellation requested.");
                    break;
                }

                var keys = string.Join(',', group.SelectMany(_ => _.Data.Keys).Distinct().Order());
                int count = group.Count();
                logger.LogInformation("Sending Record Group {count} \"{schema}\" with data keys: {keys}", count, group.Key, keys);
            }

            return Task.FromResult(true);
        }
    }
}