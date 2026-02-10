using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Concurrent;

namespace FlowCore.Simulation.Events;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public void Publish<T>(T evt)
    {
        if (_handlers.TryGetValue(typeof(T), out var list))
        {
            foreach (var d in list.ToArray())
                ((Action<T>)d)(evt);
        }
    }

    public IDisposable Subscribe<T>(Action<T> handler)
    {
        var list = _handlers.GetOrAdd(typeof(T), _ => new List<Delegate>());
        lock (list) list.Add(handler);

        return new Subscription(() =>
        {
            lock (list) list.Remove(handler);
        });
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        public Subscription(Action dispose) => _dispose = dispose;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _dispose();
        }
    }
}
