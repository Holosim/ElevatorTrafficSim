using System.Diagnostics;

using ElevatorTrafficSimulator.ConsoleHost.Publishing;
using ElevatorTrafficSimulator.ConsoleHost.Publishing.Batching;
using ElevatorTrafficSimulator.ConsoleHost.Publishing.Snapshots;

using FlowCore.Contracts.Common;
using FlowCore.Contracts.Events;
using FlowCore.Contracts.Snapshots;

using FlowCore.Domain.Building;
using FlowCore.Domain.Common;
using FlowCore.Domain.Events;
using FlowCore.Domain.People;
using FlowCore.Domain.Requests;
using FlowCore.Domain.Routing;

using FlowCore.Simulation;
using FlowCore.Simulation.Adapters;
using FlowCore.Simulation.Events;


internal static class Program
{
    private static async Task Main()
    {
        // ------------------------------------------------------------
        // Output + Publishers
        // ------------------------------------------------------------
        var outputDir = Path.Combine(AppContext.BaseDirectory, "out");
        await using var publisher = new NdjsonContractPublisher(outputDir);

        // Events: no drops, bounded memory, batched flush
        await using var eventBatcher = new EventBatcherNoDrop(
            flushAsync: batch => publisher.PublishEventsAsync(batch),
            channelCapacity: 10_000,
            maxBatchSize: 512,
            flushInterval: TimeSpan.FromMilliseconds(100));

        // Snapshots: coalescing + optional wall-time throttle
        await using var snapshotPublisher = new SnapshotPublisherCoalescing(
            publishAsync: snap => publisher.PublishSnapshotAsync(snap),
            enableWallThrottle: false,
            wallThrottlePeriod: TimeSpan.FromSeconds(1)); // 1 Hz wall-time when enabled

        // Easy toggle:
        // snapshotPublisher.EnableWallThrottle = true;

        // ------------------------------------------------------------
        // Run metadata
        // ------------------------------------------------------------
        var runId = 1;
        long runStartedSeq = 0;
        long tick = 0;

        const int floorCount = 40;
        const int elevatorCount = 4;
        const int elevatorCapacity = 16;

        const double dtSim = 0.2;                 // 5 Hz simulation tick (sim-time)
        const double simDurationSeconds = 60.0;   // run length in sim-time
        const double speedFloorsPerSecond = 1.0;  // simple movement constant for now

        // ------------------------------------------------------------
        // Emit RunStarted (contract event directly)
        // ------------------------------------------------------------
        await eventBatcher.EnqueueAsync(new SimEventRecord(
            RunId: runId,
            Sequence: Interlocked.Increment(ref runStartedSeq),
            T: 0.0,
            Type: SimEventType.RunStarted,
            Source: "ConsoleHost",
            Message: "Elevator Traffic Simulator run started",
            Payload: JsonPayload.From(new RunStartedPayload(
                FloorCount: floorCount,
                ElevatorCount: elevatorCount,
                RandomSeed: 12345,
                PlannedDurationSeconds: simDurationSeconds,
                ScenarioName: "SingleCall_Smoke",
                ContractVersion: ContractVersion.AsString()
            ))
        ));

        // ------------------------------------------------------------
        // Build Domain + Simulation
        // ------------------------------------------------------------
        var building = new Building(floorCount);

        var bus = new InMemoryEventBus();

        var elevators = new List<Elevator>(capacity: elevatorCount);
        for (int i = 0; i < elevatorCount; i++)
            elevators.Add(new Elevator(id: i + 1, capacity: elevatorCapacity, startFloor: 0));

        var strategy = new DispatchStrategyBasic();
        var elevatorController = new ElevatorController(bus, strategy, elevators);

        // Adapter: DomainEvents -> SimEventRecord -> eventBatcher (async-safe)
        await using var adapter = new ContractEventAdapterAsync(
            runId: runId,
            bus: bus,
            emitAsync: evt => eventBatcher.EnqueueAsync(evt));

        // ------------------------------------------------------------
        // Seed a single person + a single call from Lobby (0) -> 10
        // ------------------------------------------------------------
        var route = new Route(new[]
        {
            new Destination(Floor: 10, PlannedStaySeconds: 0)
        });

        var person = new Person(
            id: 1,
            type: FlowCore.Contracts.Common.PassengerType.OfficeWorker,
            startFloor: 0,
            route: route);

        // Put person into floor queue (Up)
        building.GetFloor(0).EnqueueUp(person.Id);

        // Publish QueueSizeChanged domain event so it appears in logs immediately.
        bus.Publish(new QueueSizeChangedDomainEvent(
            T: 0.0,
            Source: "FloorSystem",
            Floor: 0,
            Direction: 1, // 1=Up
            NewQueueSize: building.GetFloor(0).WaitingUpCount
        ));

        // Submit a call request
        var call = new CallRequest(
            CallId: 1,
            PersonId: person.Id,
            PersonType: person.Type,
            OriginFloor: 0,
            DestinationFloor: 10,
            Direction: 1,  // 1=Up
            RequestT: 0.0);

        elevatorController.SubmitCall(call);

        // ------------------------------------------------------------
        // Tick-based loop (sim-time). Snapshots at 5 Hz sim-time
        // ------------------------------------------------------------
        var totalTicks = (int)Math.Ceiling(simDurationSeconds / dtSim);
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < totalTicks; i++)
        {
            tick++;
            var tSim = i * dtSim;

            // Optional demo: flip wall throttle based on real elapsed time
            // (wall-time = real elapsed clock time)
            if (stopwatch.Elapsed.TotalSeconds >= 2.0)
                snapshotPublisher.EnableWallThrottle = true;

            if (stopwatch.Elapsed.TotalSeconds >= 5.0)
                snapshotPublisher.EnableWallThrottle = false;

            // 1) Move elevators
            foreach (var e in elevatorController.Fleet)
                e.Update(dtSim, speedFloorsPerSecond);

            // 2) Controller lifecycle (assign calls, board/unboard, etc.)
            elevatorController.Update(building, tSim, dtSim);

            // 3) Snapshot (coalesced publisher)
            var snap = BuildSnapshot(runId, tick, tSim, building, elevatorController.Fleet);
            snapshotPublisher.TryEnqueue(snap);
        }

        // ------------------------------------------------------------
        // Emit RunEnded (contract event directly)
        // ------------------------------------------------------------
        long runEndedSeq = runStartedSeq;

        await eventBatcher.EnqueueAsync(new SimEventRecord(
            RunId: runId,
            Sequence: Interlocked.Increment(ref runEndedSeq),
            T: simDurationSeconds,
            Type: SimEventType.RunEnded,
            Source: "ConsoleHost",
            Message: "Elevator Traffic Simulator run ended",
            Payload: JsonPayload.From(new RunEndedPayload(
                TotalPeople: 1,
                TotalCallsCompleted: 1
            ))
        ));

        Console.WriteLine($"Run complete. Output dir: {outputDir}");
        Console.WriteLine("Files: events.ndjson, snapshots.ndjson");
    }

    private static SimTickSnapshot BuildSnapshot(
        int runId,
        long tick,
        double tSim,
        Building building,
        IReadOnlyList<Elevator> fleet)
    {
        var elevators = new ElevatorSnapshot[fleet.Count];
        for (int i = 0; i < fleet.Count; i++)
        {
            var e = fleet[i];

            elevators[i] = new ElevatorSnapshot(
                VehicleId: e.Id,
                PositionFloor: e.PositionFloor,
                CurrentFloor: e.CurrentFloor,
                TargetFloor: e.TargetFloor,
                Direction: e.Direction,
                State: e.State,
                Capacity: e.Capacity,
                OccupantCount: e.OccupantCount,
                StopQueueFloors: e.StopQueueFloors.Count == 0 ? Array.Empty<int>() : e.StopQueueFloors.ToArray()
            );
        }

        var floors = new FloorQueueSnapshot[building.FloorCount];
        for (int f = 0; f < building.FloorCount; f++)
        {
            var floor = building.GetFloor(f);

            floors[f] = new FloorQueueSnapshot(
                Floor: f,
                WaitingUp: floor.WaitingUpCount,
                WaitingDown: floor.WaitingDownCount,
                CurrentOccupantsOnFloor: floor.CurrentOccupantsCount
            );
        }

        return new SimTickSnapshot(
            RunId: runId,
            Tick: tick,
            T: tSim,
            Elevators: elevators,
            Floors: floors
        );
    }
}
