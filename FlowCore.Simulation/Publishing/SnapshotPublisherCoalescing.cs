using System.Threading.Channels;
using FlowCore.Contracts.Snapshots;

namespace ElevatorTrafficSimulator.ConsoleHost.Publishing.Snapshots;

public sealed class SnapshotPublisherCoalescing : IAsyncDisposable
{
    private readonly Channel<SimTickSnapshot> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loopTask;

    private readonly Func<SimTickSnapshot, ValueTask> _publishAsync;

    public bool EnableWallThrottle { get; set; }
    public TimeSpan WallThrottlePeriod { get; set; }

    public SnapshotPublisherCoalescing(
        Func<SimTickSnapshot, ValueTask> publishAsync,
        bool enableWallThrottle = false,
        TimeSpan? wallThrottlePeriod = null)
    {
        _publishAsync = publishAsync;
        EnableWallThrottle = enableWallThrottle;
        WallThrottlePeriod = wallThrottlePeriod ?? TimeSpan.FromSeconds(1);

        // Capacity 1 + DropOldest keeps the most recent snapshot (coalescing).
        _channel = Channel.CreateBounded<SimTickSnapshot>(new BoundedChannelOptions(1)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        _loopTask = Task.Run(LoopAsync);
    }

    public bool TryEnqueue(in SimTickSnapshot snapshot) => _channel.Writer.TryWrite(snapshot);

    private async Task LoopAsync()
    {
        var token = _cts.Token;
        var reader = _channel.Reader;

        SimTickSnapshot? latest = null;

        try
        {
            while (!token.IsCancellationRequested)
            {
                if (!EnableWallThrottle)
                {
                    // Publish as fast as snapshots arrive, but always publish the latest available.
                    var first = await reader.ReadAsync(token);
                    latest = first;

                    while (reader.TryRead(out var snap))
                        latest = snap;

                    await SafePublishAsync(latest.Value);
                    latest = null;
                }
                else
                {
                    // Wall throttle: wake on cadence, publish latest if any.
                    while (reader.TryRead(out var snap))
                        latest = snap;

                    if (latest is SimTickSnapshot s)
                    {
                        await SafePublishAsync(s);
                        latest = null;
                    }

                    await Task.Delay(WallThrottlePeriod, token);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            while (reader.TryRead(out var snap))
                latest = snap;

            if (latest is SimTickSnapshot s)
                await SafePublishAsync(s);

            _channel.Writer.TryComplete();
        }
    }

    private async ValueTask SafePublishAsync(SimTickSnapshot snapshot)
    {
        try { await _publishAsync(snapshot); }
        catch { /* protect sim. add callback later if desired */ }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        await _loopTask;
        _cts.Dispose();
    }
}
