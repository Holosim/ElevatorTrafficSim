using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading.Channels;
using FlowCore.Contracts.Snapshots;

namespace FlowCore.Simulation.Publishing;

public sealed class SnapshotThrottler : IAsyncDisposable
{
    private readonly Channel<SimTickSnapshot> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loopTask;

    private readonly TimeSpan _period; // wall-time period
    private readonly Func<SimTickSnapshot, ValueTask> _publishAsync;

    /// <param name="publishAsync">Where snapshots ultimately go (file, socket, UI, etc.).</param>
    /// <param name="hzWallTime">Snapshots per second of real elapsed time. 1.0 = one per wall-time second.</param>
    /// <param name="capacity">Max buffered snapshots held in memory.</param>
    public SnapshotThrottler(
        Func<SimTickSnapshot, ValueTask> publishAsync,
        double hzWallTime = 1.0,
        int capacity = 16)
    {
        if (hzWallTime <= 0) throw new ArgumentOutOfRangeException(nameof(hzWallTime));
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

        _publishAsync = publishAsync;
        _period = TimeSpan.FromSeconds(1.0 / hzWallTime);

        _channel = Channel.CreateBounded<SimTickSnapshot>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait // <-- no drops, bounded memory, backpressure
        });

        _loopTask = Task.Run(LoopAsync);
    }

    /// <summary>
    /// Enqueue a snapshot. If the buffer is full, this awaits until space is available.
    /// This is the backpressure point that prevents drops.
    /// </summary>
    public ValueTask EnqueueAsync(in SimTickSnapshot snapshot, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(snapshot, ct);

    private async Task LoopAsync()
    {
        var token = _cts.Token;
        var reader = _channel.Reader;

        try
        {
            while (!token.IsCancellationRequested)
            {
                // 1 Hz wall-time throttle: wake once per period, publish exactly one snapshot if available.
                await Task.Delay(_period, token);

                // Wait until at least one snapshot is available, then publish the next one in order.
                // This preserves every snapshot (no drops), but can build backlog if producer is faster.
                var snap = await reader.ReadAsync(token);
                await SafePublishAsync(snap);
            }
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
        finally
        {
            // Drain remaining snapshots on shutdown (best effort).
            while (reader.TryRead(out var snap))
                await SafePublishAsync(snap);

            _channel.Writer.TryComplete();
        }
    }

    private async ValueTask SafePublishAsync(SimTickSnapshot snapshot)
    {
        try
        {
            await _publishAsync(snapshot);
        }
        catch
        {
            // v1: swallow to avoid crashing the sim on logging errors.
            // Later: expose OnError callback.
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        await _loopTask;
        _cts.Dispose();
    }
}



//using System.Threading.Channels;
//using FlowCore.Contracts.Snapshots;

//namespace FlowCore.Simulation.Publishing;

//public sealed class SnapshotThrottler : IAsyncDisposable
//{
//    private readonly Channel<SimTickSnapshot> _channel;
//    private readonly CancellationTokenSource _cts = new();
//    private readonly Task _loopTask;

//    private readonly TimeSpan _period; // wall-time period
//    private readonly Func<SimTickSnapshot, ValueTask> _publishAsync;

//    /// <param name="publishAsync">Where a snapshot ultimately goes (file, socket, UI, etc.).</param>
//    /// <param name="hzWallTime">Snapshots per second of real time. 1.0 = one per wall-time second.</param>
//    /// <param name="capacity">Max buffered snapshots held in memory.</param>
//    public SnapshotThrottler(
//        Func<SimTickSnapshot, ValueTask> publishAsync,
//        double hzWallTime = 1.0,
//        int capacity = 4)
//    {
//        if (hzWallTime <= 0) throw new ArgumentOutOfRangeException(nameof(hzWallTime));
//        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

//        _publishAsync = publishAsync;
//        _period = TimeSpan.FromSeconds(1.0 / hzWallTime);

//        // Bounded channel caps memory
//        _channel = Channel.CreateBounded<SimTickSnapshot>(new BoundedChannelOptions(capacity)
//        {
//            SingleReader = true,
//            SingleWriter = false,

//            // Drop policy: when full, drop oldest to make room for newer snapshots
//            FullMode = BoundedChannelFullMode.DropOldest
//        });

//        _loopTask = Task.Run(LoopAsync);
//    }

//    /// <summary>
//    /// Enqueue a snapshot for throttled publication.
//    /// Returns false if the channel writer is completed or snapshot could not be queued.
//    /// With DropOldest, TryWrite generally succeeds unless completed.
//    /// </summary>
//    public bool TryEnqueue(in SimTickSnapshot snapshot)
//    {
//        return _channel.Writer.TryWrite(snapshot);
//    }

//    private async Task LoopAsync()
//    {
//        var token = _cts.Token;
//        var reader = _channel.Reader;

//        SimTickSnapshot? latest = null;

//        try
//        {
//            // Throttle by wall-time: wake once per period, then publish the most recent snapshot seen.
//            while (!token.IsCancellationRequested)
//            {
//                // Drain whatever is available right now and keep only the most recent.
//                while (reader.TryRead(out var snap))
//                    latest = snap;

//                // If we have a snapshot, publish it.
//                if (latest is SimTickSnapshot s)
//                {
//                    await SafePublishAsync(s);
//                    latest = null;
//                }

//                await Task.Delay(_period, token);
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            // expected on shutdown
//        }
//        finally
//        {
//            // Best-effort final drain + publish latest
//            while (reader.TryRead(out var snap))
//                latest = snap;

//            if (latest is SimTickSnapshot s)
//                await SafePublishAsync(s);

//            _channel.Writer.TryComplete();
//        }
//    }

//    private async ValueTask SafePublishAsync(SimTickSnapshot snapshot)
//    {
//        try
//        {
//            await _publishAsync(snapshot);
//        }
//        catch
//        {
//            // v1 policy: swallow so visualization/logging cannot stall the sim.
//            // Option later: expose OnError callback.
//        }
//    }

//    public async ValueTask DisposeAsync()
//    {
//        _cts.Cancel();
//        await _loopTask;
//        _cts.Dispose();
//    }
//}

