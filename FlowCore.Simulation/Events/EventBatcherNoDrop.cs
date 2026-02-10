using System.Threading.Channels;
using FlowCore.Contracts.Events;

namespace ElevatorTrafficSimulator.ConsoleHost.Publishing.Batching;

public sealed class EventBatcherNoDrop : IAsyncDisposable
{
    private readonly Channel<SimEventRecord> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loopTask;

    private readonly int _maxBatchSize;
    private readonly TimeSpan _flushInterval;
    private readonly Func<IReadOnlyList<SimEventRecord>, ValueTask> _flushAsync;

    public EventBatcherNoDrop(
        Func<IReadOnlyList<SimEventRecord>, ValueTask> flushAsync,
        int channelCapacity = 10_000,
        int maxBatchSize = 512,
        TimeSpan? flushInterval = null)
    {
        if (channelCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(channelCapacity));
        if (maxBatchSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxBatchSize));

        _flushAsync = flushAsync;
        _maxBatchSize = maxBatchSize;
        _flushInterval = flushInterval ?? TimeSpan.FromMilliseconds(100);

        _channel = Channel.CreateBounded<SimEventRecord>(new BoundedChannelOptions(channelCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait // no drops
        });

        _loopTask = Task.Run(LoopAsync);
    }

    public ValueTask EnqueueAsync(in SimEventRecord evt, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(evt, ct);

    private async Task LoopAsync()
    {
        var token = _cts.Token;
        var reader = _channel.Reader;

        var batch = new List<SimEventRecord>(_maxBatchSize);

        try
        {
            while (!token.IsCancellationRequested)
            {
                // Wait for at least one event
                if (!await reader.WaitToReadAsync(token))
                    break;

                batch.Clear();

                // Drain quickly up to maxBatchSize
                while (batch.Count < _maxBatchSize && reader.TryRead(out var evt))
                    batch.Add(evt);

                if (batch.Count > 0)
                    await SafeFlushAsync(batch);

                // Allow small batching window
                await Task.Delay(_flushInterval, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            // Final drain
            batch.Clear();
            while (reader.TryRead(out var evt))
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

            _channel.Writer.TryComplete();
        }
    }

    private async ValueTask SafeFlushAsync(IReadOnlyList<SimEventRecord> batch)
    {
        try { await _flushAsync(batch); }
        catch { /* protect sim. add callback later if desired */ }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        await _loopTask;
        _cts.Dispose();
    }
}
