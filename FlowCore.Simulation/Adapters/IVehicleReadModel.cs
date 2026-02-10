using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowCore.Contracts.Common;

namespace FlowCore.Simulation.Adapters;

public interface IVehicleReadModel
{
    int VehicleId { get; }
    double PositionFloor { get; }          // continuous for smooth Unreal motion
    int CurrentFloor { get; }
    int? TargetFloor { get; }
    MotionDirection Direction { get; }
    VehicleState State { get; }
    int Capacity { get; }
    int OccupantCount { get; }
    IReadOnlyList<int> StopQueueFloors { get; }
}
