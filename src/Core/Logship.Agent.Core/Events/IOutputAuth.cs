

namespace Logship.Agent.Core.Events
{
    public interface IOutputAuth
    {
        ValueTask<bool> TryAddAuthAsync(HttpRequestMessage requestMessage, CancellationToken token);

        ValueTask InvalidateAsync(CancellationToken token);
    }
}
