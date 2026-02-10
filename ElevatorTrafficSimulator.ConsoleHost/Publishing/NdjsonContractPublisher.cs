using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;
using FlowCore.Contracts.Events;
using FlowCore.Contracts.Snapshots;
using System.Text.Json;
using FlowCore.Contracts.Events;

namespace ElevatorTrafficSimulator.ConsoleHost.Publishing;


public sealed class NdjsonContractPublisher : IContractPublisher
{
    private readonly StreamWriter _eventsWriter;
    private readonly StreamWriter _snapshotsWriter;
    private readonly JsonSerializerOptions _jsonOptions;

    public NdjsonContractPublisher(string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        _eventsWriter = new StreamWriter(File.Open(Path.Combine(outputDir, "events.ndjson"), FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true
        };

        _snapshotsWriter = new StreamWriter(File.Open(Path.Combine(outputDir, "snapshots.ndjson"), FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true
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

    public async ValueTask PublishSnapshotAsync(SimTickSnapshot snap, CancellationToken ct = default)
    {
        var line = JsonSerializer.Serialize(snap, _jsonOptions);
        await _snapshotsWriter.WriteLineAsync(line.AsMemory(), ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _eventsWriter.DisposeAsync();
        await _snapshotsWriter.DisposeAsync();
    }


    public async ValueTask PublishEventsAsync(IReadOnlyList<SimEventRecord> events, CancellationToken ct = default)
    {
        foreach (var evt in events)
        {
            var line = JsonSerializer.Serialize(evt, _jsonOptions);
            await _eventsWriter.WriteLineAsync(line.AsMemory(), ct);
        }

        // optional: flush per batch rather than per line
        await _eventsWriter.FlushAsync();
    }
}
