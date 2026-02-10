using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowCore.Contracts.Snapshots;
using FlowCore.Domain.Building;

namespace FlowCore.Simulation.Adapters;

/// <summary>
/// Converts mutable Domain state into immutable Contracts snapshots.
/// Copies all data. Does not return or store references to Domain collections.
/// </summary>
public sealed class SimSnapshotAssembler
{
    public SimTickSnapshot CreateTickSnapshot(
        int runId,
        long tick,
        double tSeconds,
        IBuildingReadModel building,
        IReadOnlyList<IVehicleReadModel> vehicles)
    {
        // Floors: produce new snapshots with primitive values only
        var floorSnapshots = new FloorQueueSnapshot[building.FloorCount];

        for (var i = 0; i < building.FloorCount; i++)
        {
            Floor f = building.Floors[i];

            // IMPORTANT: do not expose queues/lists directly
            // Just read counts and copy primitives.
            floorSnapshots[i] = new FloorQueueSnapshot(
                Floor: f.FloorNumber,
                WaitingUp: f.WaitingUpCalls.Count,
                WaitingDown: f.WaitingDownCalls.Count,
                CurrentOccupantsOnFloor: f.CurrentOccupants.Count
            );
        }

        // Vehicles: produce new snapshots and copy stop queues
        var elevatorSnapshots = new ElevatorSnapshot[vehicles.Count];

        for (var i = 0; i < vehicles.Count; i++)
        {
            var v = vehicles[i];

            // Copy the stop queue into a brand-new int[]
            var stopQueueCopy = v.StopQueueFloors.Count == 0
                ? Array.Empty<int>()
                : v.StopQueueFloors.ToArray();

            elevatorSnapshots[i] = new ElevatorSnapshot(
                VehicleId: v.VehicleId,
                PositionFloor: v.PositionFloor,
                CurrentFloor: v.CurrentFloor,
                TargetFloor: v.TargetFloor,
                Direction: v.Direction,
                State: v.State,
                Capacity: v.Capacity,
                OccupantCount: v.OccupantCount,
                StopQueueFloors: stopQueueCopy
            );
        }

        // Contracts snapshot contains only immutable record structs and new arrays
        return new SimTickSnapshot(
            RunId: runId,
            Tick: tick,
            T: tSeconds,
            Elevators: elevatorSnapshots,
            Floors: floorSnapshots
        );
    }
}
