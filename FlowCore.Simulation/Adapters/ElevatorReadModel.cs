using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowCore.Contracts.Common;

namespace FlowCore.Simulation.Adapters;

public sealed class ElevatorReadModel : IVehicleReadModel
{
    /* Why add this line?  What does it do?*/
    //private readonly ElevatorTrafficSim = FlowCore.Simulation.Elevator;
    private readonly Elevator _elevator;

    public ElevatorReadModel(global::FlowCore.Simulation.Elevator elevator) => _elevator = elevator;

    public int VehicleId => _elevator.Id;
    public double PositionFloor => _elevator.PositionFloor;
    public int CurrentFloor => _elevator.CurrentFloor;
    public int? TargetFloor => _elevator.TargetFloor;
    public MotionDirection Direction => _elevator.Direction;
    public VehicleState State => _elevator.State;
    public int Capacity => _elevator.Capacity;
    public int OccupantCount => _elevator.OccupantCount;

    public IReadOnlyList<int> StopQueueFloors => _elevator.StopQueueFloors.Count == 0
        ? Array.Empty<int>()
        : _elevator.StopQueueFloors.ToArray();
}
