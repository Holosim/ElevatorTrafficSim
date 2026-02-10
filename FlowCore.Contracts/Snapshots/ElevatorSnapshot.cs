using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowCore.Contracts.Common;

namespace FlowCore.Contracts.Snapshots;

public readonly record struct ElevatorSnapshot(
    int VehicleId,
    double PositionFloor,
    int CurrentFloor,
    int? TargetFloor,
    MotionDirection Direction,
    VehicleState State,
    int Capacity,
    int OccupantCount,
    IReadOnlyList<int> StopQueueFloors
);

