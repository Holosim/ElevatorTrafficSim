using FlowCore.Domain.Building;
using FlowCore.Domain.Events;
using FlowCore.Domain.Requests;
using FlowCore.Simulation.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Simulation;

public sealed class ElevatorController
{
    private readonly IEventBus _bus;
    private readonly IElevatorDispatchStrategy _strategy;
    private readonly List<Elevator> _fleet;

    // Minimal: pending calls FIFO
    private readonly Queue<CallRequest> _pendingCalls = new();

    // Minimal: one active call per elevator
    private readonly Dictionary<int, CallRequest> _activeCallByElevatorId = new();

    public ElevatorController(IEventBus bus, IElevatorDispatchStrategy strategy, IEnumerable<Elevator> fleet)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _fleet = fleet?.ToList() ?? throw new ArgumentNullException(nameof(fleet));
        if (_fleet.Count == 0) throw new ArgumentException("Fleet must not be empty.", nameof(fleet));
    }

    public IReadOnlyList<Elevator> Fleet => _fleet;

    public void SubmitCall(in CallRequest call) => _pendingCalls.Enqueue(call);

    public void Update(Building building, double tSim, double dtSimSeconds)
    {
        // 1) Assign pending calls to idle elevators
        while (_pendingCalls.Count > 0)
        {
            var call = _pendingCalls.Peek();

            var elevatorId = _strategy.SelectElevator(_fleet, call);
            var elevator = _fleet.First(e => e.Id == elevatorId);

            // only assign if elevator not already handling a call
            if (_activeCallByElevatorId.ContainsKey(elevatorId))
                break;

            _pendingCalls.Dequeue();
            _activeCallByElevatorId[elevatorId] = call;

            // Publish domain event (adapter will convert)
            _bus.Publish(new CallAssignedDomainEvent(
                T: tSim,
                Source: "ElevatorController",
                CallId: call.CallId,
                VehicleId: elevatorId,
                EstimatedPickupT: double.NaN // compute later
            ));

            elevator.AssignTarget(call.OriginFloor);
        }

        // 2) For each elevator with an active call, advance its call lifecycle
        foreach (var elevator in _fleet)
        {
            if (!_activeCallByElevatorId.TryGetValue(elevator.Id, out var call))
                continue;

            // If elevator arrived at origin and doors are open, board 1 person for now (v1)
            if (elevator.State == FlowCore.Contracts.Common.VehicleState.DoorsOpen
                && elevator.CurrentFloor == call.OriginFloor
                && !elevator.ContainsPassenger(call.PersonId))
            {
                // Begin boarding time (1 person => 1 second)
                elevator.BeginBoarding(1);

                // Remove from floor queue (minimal: direction-based)
                var floor = building.GetFloor(call.OriginFloor);
                // For v1: assume the person is at front. Later: explicit person removal.
                if (call.Direction == 1 && floor.WaitingUpCount > 0) floor.DequeueUp();
                if (call.Direction == 2 && floor.WaitingDownCount > 0) floor.DequeueDown();

                elevator.AddPassenger(call.PersonId);

                _bus.Publish(new PersonBoardedDomainEvent(
                    T: tSim,
                    Source: $"Elevator#{elevator.Id}",
                    PersonId: call.PersonId,
                    CallId: call.CallId,
                    VehicleId: elevator.Id,
                    Floor: call.OriginFloor,
                    VehicleOccupantCountAfter: elevator.OccupantCount
                ));

                continue;
            }

            // If boarding finished, send elevator to destination
            if (elevator.State == FlowCore.Contracts.Common.VehicleState.Loading
                && elevator.StateTimeRemaining <= 0
                && elevator.ContainsPassenger(call.PersonId))
            {
                elevator.CloseDoors();
                elevator.AssignTarget(call.DestinationFloor);
                continue;
            }

            // If elevator arrived at destination, unload
            if (elevator.State == FlowCore.Contracts.Common.VehicleState.DoorsOpen
                && elevator.CurrentFloor == call.DestinationFloor
                && elevator.ContainsPassenger(call.PersonId))
            {
                elevator.BeginUnloading(1);

                // We remove immediately but you could delay until unload completes.
                elevator.RemovePassenger(call.PersonId);

                // Optional domain event: PersonAlightedDomainEvent (add later)
                // For now keep it minimal so you can see board->ride->arrive.

                continue;
            }

            // If unloading complete, mark call complete and idle
            if (elevator.State == FlowCore.Contracts.Common.VehicleState.Unloading
                && elevator.StateTimeRemaining <= 0
                && !elevator.ContainsPassenger(call.PersonId))
            {
                elevator.CloseDoors();

                // Done
                _activeCallByElevatorId.Remove(elevator.Id);
                // Elevator idle for now. Later: return-to-lobby policy.
                // If no target, set idle.
                // We'll leave it as DoorsClosed; next tick it will be available again.

                continue;
            }
        }
    }
}

