using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowCore.Contracts.Common;
// using FlowCore.Domain.Vehicles; // wherever Elevator ends up

namespace FlowCore.Simulation.Adapters;
/*
public sealed class ElevatorReadModel : IVehicleReadModel
{
    private readonly Elevator _elevator;

    public ElevatorReadModel(Elevator elevator) => _elevator = elevator;

    public int VehicleId => _elevator.Id;
    public double PositionFloor => _elevator.PositionFloor;
    public int CurrentFloor => _elevator.CurrentFloor;
    public int? TargetFloor => _elevator.TargetFloor;
    public MotionDirection Direction => _elevator.Direction;
    public VehicleState State => _elevator.State;
    public int Capacity => _elevator.Capacity;
    public int OccupantCount => _elevator.OccupantCount;

    // CRITICAL: return a copy, never expose internal list
    public IReadOnlyList<int> StopQueueFloors => _elevator.StopQueueFloors.ToArray();
}
*/