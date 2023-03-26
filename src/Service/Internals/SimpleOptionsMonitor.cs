using Microsoft.Extensions.Options;

namespace Logship.Agent.Service.Internals
{
#pragma warning disable IL2091 // Target generic argument does not satisfy 'DynamicallyAccessedMembersAttribute' in target method or type. The generic parameter of the source method or type does not have matching annotations.
    internal class SimpleOptionsMonitor<T> : IOptionsMonitor<T>
#pragma warning restore IL2091 // Target generic argument does not satisfy 'DynamicallyAccessedMembersAttribute' in target method or type. The generic parameter of the source method or type does not have matching annotations.
    {
        public T CurrentValue { get; }

        public SimpleOptionsMonitor(T instance)
        {
            this.CurrentValue = instance;
        }

        public T Get(string? name)
        {
            return this.CurrentValue;
        }

        public IDisposable? OnChange(Action<T, string?> listener)
        {
            return null;
        }
    }
}
