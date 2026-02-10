using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Concurrent;
using FlowCore.Contracts.Events;

namespace FlowCore.Simulation.Publishing;

public sealed class EventBatcher : IAsyncDisposable
{
    private readonly ConcurrentQueue<SimEventRecord> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly CancellationTokenSource _cts = new();

    private readonly int _maxBatchSize;
    private readonly TimeSpan _flushInterval;

    private readonly Func<IReadOnlyList<SimEventRecord>, ValueTask> _flushAsync;

    private readonly Task _loopTask;

    public EventBatcher(
        Func<IReadOnlyList<SimEventRecord>, ValueTask> flushAsync,
        int maxBatchSize = 512,
        TimeSpan? flushInterval = null)
    {
        _flushAsync = flushAsync;
        _maxBatchSize = Math.Max(1, maxBatchSize);
        _flushInterval = flushInterval ?? TimeSpan.FromMilliseconds(100);

        _loopTask = Task.Run(LoopAsync);
    }

    public ValueTask EnqueueAsync(SimEventRecord evt)
    {
        _queue.Enqueue(evt);
        _signal.Release();
        return ValueTask.CompletedTask;
    }

    private async Task LoopAsync()
    {
        var token = _cts.Token;

        // Reusable buffer to reduce allocations
        var batch = new List<SimEventRecord>(_maxBatchSize);

        try
        {
            while (!token.IsCancellationRequested)
            {
                // Wait for at least one event or timeout to flush partial batch
                await _signal.WaitAsync(_flushInterval, token);

                // Drain up to maxBatchSize
                batch.Clear();
                while (batch.Count < _maxBatchSize && _queue.TryDequeue(out var evt))
                    batch.Add(evt);

                if (batch.Count > 0)
                {
                    await _flushAsync(batch);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
        finally
        {
            // Final flush: drain everything
            batch.Clear();
            while (_queue.TryDequeue(out var evt))
            {
                batch.Add(evt);
                if (batch.Count >= _maxBatchSize)
                {
                    await SafeFlushAsync(batch);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
                await SafeFlushAsync(batch);
        }
    }

    private async ValueTask SafeFlushAsync(IReadOnlyList<SimEventRecord> batch)
    {
        try
        {
            await _flushAsync(batch);
        }
        catch
        {
            // In v1: swallow to protect the sim loop from logging failure.
            // Later: route to an error handler or fallback file.
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _signal.Release(); // wake loop
        await _loopTask;
        _signal.Dispose();
        _cts.Dispose();
    }
}

