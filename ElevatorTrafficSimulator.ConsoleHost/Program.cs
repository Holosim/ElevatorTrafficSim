using ElevatorTrafficSimulator.ConsoleHost.Publishing;
using ElevatorTrafficSimulator.ConsoleHost.Publishing.Batching;
using ElevatorTrafficSimulator.ConsoleHost.Publishing.Snapshots;
using FlowCore.Contracts.Common;
using FlowCore.Contracts.Events;
using FlowCore.Contracts.Snapshots;
using FlowCore.Simulation.Publishing;
using System.Diagnostics;

internal static class Program
{
    private static async Task Main()
    {
        var outputDir = Path.Combine(AppContext.BaseDirectory, "out");
        await using var publisher = new NdjsonContractPublisher(outputDir);

        // Events: no drops, bounded memory, batched flush
        await using var eventBatcher = new EventBatcherNoDrop(
            flushAsync: batch => publisher.PublishEventsAsync(batch),
            channelCapacity: 10_000,
            maxBatchSize: 512,
            flushInterval: TimeSpan.FromMilliseconds(100));

        // Snapshots: coalescing, optional wall-time throttle
        await using var snapshotPublisher = new SnapshotPublisherCoalescing(
            publishAsync: snap => publisher.PublishSnapshotAsync(snap),
            enableWallThrottle: false,
            wallThrottlePeriod: TimeSpan.FromSeconds(1)); // 1 Hz wall-time when enabled

        // Toggle wall-time throttling easily here
        // snapshotPublisher.EnableWallThrottle = true;

        var runId = 1;
        long seq = 0;
        long tick = 0;

        // Emit RunStarted event
        await eventBatcher.EnqueueAsync(new SimEventRecord(
            RunId: runId,
            Sequence: Interlocked.Increment(ref seq),
            T: 0.0,
            Type: SimEventType.RunStarted,
            Source: "ConsoleHost",
            Message: "Smoke test run started",
            Payload: JsonPayload.From(new RunStartedPayload(
                FloorCount: 40,
                ElevatorCount: 4,
                RandomSeed: 12345,
                PlannedDurationSeconds: 60.0,
                ScenarioName: "SmokeTest",
                ContractVersion: ContractVersion.AsString()
            ))
        ));

        // Tick-based sim: 5 Hz
        // This means dtSim = 0.2 seconds of simulation time per tick.
        var dtSim = 0.2;
        var simDurationSeconds = 10.0; // short smoke test
        var totalTicks = (int)Math.Ceiling(simDurationSeconds / dtSim);

        // Dummy state for snapshot generation
        var elevatorCount = 4;
        var floorCount = 40;

        var elevatorPositions = new double[elevatorCount]; // position in floors
        var elevatorTargets = new int?[elevatorCount];
        var elevatorDirections = new MotionDirection[elevatorCount];
        var elevatorStates = new VehicleState[elevatorCount];
        var elevatorOccupants = new int[elevatorCount];
        var elevatorCapacities = new int[elevatorCount];

        for (int i = 0; i < elevatorCount; i++)
        {
            elevatorPositions[i] = 0.0;
            elevatorTargets[i] = 10 + i * 2;
            elevatorDirections[i] = MotionDirection.Up;
            elevatorStates[i] = VehicleState.Moving;
            elevatorOccupants[i] = 0;
            elevatorCapacities[i] = 16;
        }

        var waitingUp = new int[floorCount];
        var waitingDown = new int[floorCount];

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < totalTicks; i++)
        {
            tick++;
            var tSim = i * dtSim;

            // Example wall-time toggle during the run
            // Turn throttling on after 2 seconds of real time
            if (stopwatch.Elapsed.TotalSeconds >= 2.0)
                snapshotPublisher.EnableWallThrottle = true;

            // Turn throttling off again after 5 seconds of real time
            if (stopwatch.Elapsed.TotalSeconds >= 5.0)
                snapshotPublisher.EnableWallThrottle = false;

            // Fake some queue churn
            var lobby = 0;
            waitingUp[lobby] = (int)(5 + 3 * Math.Sin(tSim));
            waitingDown[10] = (int)(2 + 2 * Math.Cos(tSim));

            // Move elevators toward their target at 1 floor per second (for smoke test)
            // dtSim is 0.2s, so delta floors = 0.2 floors per tick
            var speedFloorsPerSecond = 1.0;
            var deltaFloors = speedFloorsPerSecond * dtSim;

            for (int e = 0; e < elevatorCount; e++)
            {
                var target = elevatorTargets[e] ?? 0;
                var pos = elevatorPositions[e];

                if (Math.Abs(pos - target) < 1e-6)
                {
                    // Arrived. Emit an event occasionally
                    await eventBatcher.EnqueueAsync(new SimEventRecord(
                        RunId: runId,
                        Sequence: Interlocked.Increment(ref seq),
                        T: tSim,
                        Type: SimEventType.ElevatorArrived,
                        Source: $"Elevator#{e + 1}",
                        Message: $"Arrived at floor {target}",
                        Payload: JsonPayload.From(new ElevatorArrivedPayload(
                            VehicleId: e + 1,
                            Floor: target,
                            Direction: elevatorDirections[e]
                        ))
                    ));

                    // Flip target for demo
                    elevatorTargets[e] = target == 0 ? 20 + e : 0;
                    elevatorDirections[e] = elevatorTargets[e] > target ? MotionDirection.Up : MotionDirection.Down;
                    elevatorStates[e] = VehicleState.Moving;

                    // Also emit a queue size change event for analytics plumbing
                    await eventBatcher.EnqueueAsync(new SimEventRecord(
                        RunId: runId,
                        Sequence: Interlocked.Increment(ref seq),
                        T: tSim,
                        Type: SimEventType.QueueSizeChanged,
                        Source: "FloorSystem",
                        Message: "Lobby up-queue changed",
                        Payload: JsonPayload.From(new QueueSizeChangedPayload(
                            Floor: 0,
                            Direction: MotionDirection.Up,
                            NewQueueSize: waitingUp[0]
                        ))
                    ));
                }
                else
                {
                    // Move toward target
                    if (pos < target)
                    {
                        elevatorDirections[e] = MotionDirection.Up;
                        pos = Math.Min(target, pos + deltaFloors);
                    }
                    else
                    {
                        elevatorDirections[e] = MotionDirection.Down;
                        pos = Math.Max(target, pos - deltaFloors);
                    }

                    elevatorPositions[e] = pos;
                }
            }

            // Build and enqueue snapshot at 5 Hz sim-time (every tick here)
            var snap = BuildSnapshot(runId, tick, tSim,
                elevatorPositions, elevatorTargets, elevatorDirections, elevatorStates, elevatorCapacities, elevatorOccupants,
                waitingUp, waitingDown);

            snapshotPublisher.TryEnqueue(snap);

            // This smoke test runs as fast as possible.
            // If you want to see wall-time throttling behavior more clearly,
            // you can slow it down a bit with Task.Delay(20) or similar.
        }

        // Emit RunEnded event
        await eventBatcher.EnqueueAsync(new SimEventRecord(
            RunId: runId,
            Sequence: Interlocked.Increment(ref seq),
            T: simDurationSeconds,
            Type: SimEventType.RunEnded,
            Source: "ConsoleHost",
            Message: "Smoke test run ended",
            Payload: JsonPayload.From(new RunEndedPayload(
                TotalPeople: 0,
                TotalCallsCompleted: 0
            ))
        ));

        Console.WriteLine($"Smoke test complete. Output dir: {outputDir}");
        Console.WriteLine("Files: events.ndjson, snapshots.ndjson");
        Console.WriteLine("Wall throttle toggled on/off during run for demonstration.");
    }

    private static SimTickSnapshot BuildSnapshot(
        int runId,
        long tick,
        double tSim,
        double[] elevatorPositions,
        int?[] elevatorTargets,
        MotionDirection[] elevatorDirections,
        VehicleState[] elevatorStates,
        int[] elevatorCapacities,
        int[] elevatorOccupants,
        int[] waitingUp,
        int[] waitingDown)
    {
        var elevators = new ElevatorSnapshot[elevatorPositions.Length];
        for (int i = 0; i < elevatorPositions.Length; i++)
        {
            var currentFloor = (int)Math.Round(elevatorPositions[i], MidpointRounding.AwayFromZero);

            elevators[i] = new ElevatorSnapshot(
                VehicleId: i + 1,
                PositionFloor: elevatorPositions[i],
                CurrentFloor: currentFloor,
                TargetFloor: elevatorTargets[i],
                Direction: elevatorDirections[i],
                State: elevatorStates[i],
                Capacity: elevatorCapacities[i],
                OccupantCount: elevatorOccupants[i],
                StopQueueFloors: Array.Empty<int>()
            );
        }

        var floors = new FloorQueueSnapshot[waitingUp.Length];
        for (int f = 0; f < waitingUp.Length; f++)
        {
            floors[f] = new FloorQueueSnapshot(
                Floor: f,
                WaitingUp: waitingUp[f],
                WaitingDown: waitingDown[f],
                CurrentOccupantsOnFloor: 0
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
