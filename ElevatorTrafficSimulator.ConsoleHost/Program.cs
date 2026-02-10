using FlowCore.Domain.Common;
using FlowCore.Domain.Routing;
using FlowCore.Domain.People;
using FlowCore.Domain.Building;
using FlowCore.Contracts.Common;
using FlowCore.Contracts.Events;
using FlowCore.Contracts.Snapshots;
using ElevatorTrafficSimulator.ConsoleHost.Publishing;
using FlowCore.Simulation.Events;
using FlowCore.Simulation.Publishing;
using FlowCore.Simulation.Adapters;
using FlowCore.Domain.Events;

var outputDir = Path.Combine(AppContext.BaseDirectory, "out");
await using var publisher = new NdjsonContractPublisher(outputDir);

var bus = new InMemoryEventBus();

var runId = 1;

// Create the batcher. It owns the async background flush loop.
await using var batcher = new EventBatcher(
    flushAsync: batch => publisher.PublishEventsAsync(batch),
    maxBatchSize: 512,
    flushInterval: TimeSpan.FromMilliseconds(100));

// Adapter sends events into the batcher immediately (no blocking).
await using var adapter = new ContractEventAdapterAsync(
    runId,
    bus,
    emitAsync: evt => batcher.EnqueueAsync(evt));

// Simulate a burst of events quickly
for (int i = 0; i < 10_000; i++)
{
    bus.Publish(new CallAssignedDomainEvent(
        T: i * 0.01,
        Source: "ElevatorController",
        CallId: 1000 + i,
        VehicleId: (i % 4) + 1,
        EstimatedPickupT: i * 0.01 + 2.0));
}

Console.WriteLine("Published 10k events (batched).");

// Dispose will flush remaining events


