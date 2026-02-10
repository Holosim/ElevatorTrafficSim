using System.Text.Json;
using FlowCore.Contracts.Events;
using FlowCore.Contracts.Snapshots;

namespace ElevatorTrafficSimulator.ConsoleHost.Publishing;

public sealed class NdjsonContractPublisher : IAsyncDisposable
{
    private readonly StreamWriter _eventsWriter;
    private readonly StreamWriter _snapshotsWriter;
    private readonly JsonSerializerOptions _jsonOptions;

    public NdjsonContractPublisher(string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        _eventsWriter = new StreamWriter(File.Open(
            Path.Combine(outputDir, "events.ndjson"),
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read))
        {
            AutoFlush = false
        };

        _snapshotsWriter = new StreamWriter(File.Open(
            Path.Combine(outputDir, "snapshots.ndjson"),
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read))
        {
            AutoFlush = false
        };

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false
        };
    }

    public async ValueTask PublishEventAsync(SimEventRecord evt, CancellationToken ct = default)
    {
        var line = JsonSerializer.Serialize(evt, _jsonOptions);
        await _eventsWriter.WriteLineAsync(line.AsMemory(), ct);
    }

    public async ValueTask PublishEventsAsync(IReadOnlyList<SimEventRecord> events, CancellationToken ct = default)
    {
        foreach (var evt in events)
        {
            var line = JsonSerializer.Serialize(evt, _jsonOptions);
            await _eventsWriter.WriteLineAsync(line.AsMemory(), ct);
        }
        await _eventsWriter.FlushAsync();
    }

    public async ValueTask PublishSnapshotAsync(SimTickSnapshot snap, CancellationToken ct = default)
    {
        var line = JsonSerializer.Serialize(snap, _jsonOptions);
        await _snapshotsWriter.WriteLineAsync(line.AsMemory(), ct);
        await _snapshotsWriter.FlushAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _eventsWriter.FlushAsync();
        await _snapshotsWriter.FlushAsync();
        await _eventsWriter.DisposeAsync();
        await _snapshotsWriter.DisposeAsync();
    }
}
