using FlowCore.Domain.Building;
using FlowCore.Domain.Requests;
using FlowCore.Simulation.Events;

namespace FlowCore.Simulation;

public sealed class ElevatorController
{
    private readonly IEventBus _bus;
    private readonly IElevatorDispatchStrategy _strategy;
    private readonly List<Elevator> _fleet;

    private readonly Queue<CallRequest> _pendingCalls = new();
    private readonly Dictionary<int, ActiveAssignment> _active = new();

    // Tunables for v1
    private const double DoorDwellSeconds = 2.0;

    private sealed class ActiveAssignment
    {
        public ActiveAssignment(CallRequest call)
        {
            Call = call;
            Phase = AssignmentPhase.GoingToPickup;
            HasDequeuedFromFloorQueue = false;
            HasBoarded = false;
            HasUnloaded = false;
            DoorDwellStartedAtPickup = false;
            DoorDwellStartedAtDropoff = false;
        }

        public CallRequest Call { get; }
        public AssignmentPhase Phase { get; set; }

        public bool HasDequeuedFromFloorQueue { get; set; }
        public bool HasBoarded { get; set; }
        public bool HasUnloaded { get; set; }

        public bool DoorDwellStartedAtPickup { get; set; }
        public bool DoorDwellStartedAtDropoff { get; set; }
    }

    private enum AssignmentPhase
    {
        GoingToPickup,
        DoorDwellAtPickup,
        Boarding,
        GoingToDropoff,
        DoorDwellAtDropoff,
        Unloading,
        Complete
    }

    public ElevatorController(IEventBus bus, IElevatorDispatchStrategy strategy, IEnumerable<Elevator> fleet)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _fleet = fleet?.ToList() ?? throw new ArgumentNullException(nameof(fleet));
        if (_fleet.Count == 0) throw new ArgumentException("Fleet must not be empty.", nameof(fleet));
    }

    public IReadOnlyList<Elevator> Fleet => _fleet;

    public void SubmitCall(CallRequest call) => _pendingCalls.Enqueue(call);

    public void Update(Building building, double tSim, double dtSimSeconds)
    {
        if (building is null) throw new ArgumentNullException(nameof(building));

        AssignPendingCalls(tSim);

        foreach (var elevator in _fleet)
        {
            if (!_active.TryGetValue(elevator.Id, out var assignment))
                continue;

            StepAssignment(building, elevator, assignment, tSim);
            if (assignment.Phase == AssignmentPhase.Complete)
                _active.Remove(elevator.Id);
        }
    }

    private void AssignPendingCalls(double tSim)
    {
        // Assign pending calls to elevators that have no active assignment.
        while (_pendingCalls.Count > 0)
        {
            var call = _pendingCalls.Peek();

            var elevatorId = _strategy.SelectElevator(_fleet, call);

            if (_active.ContainsKey(elevatorId))
                break; // best candidate is busy. stop assigning for now.

            var elevator = _fleet.First(e => e.Id == elevatorId);

            _pendingCalls.Dequeue();
            _active[elevatorId] = new ActiveAssignment(call);

            // If you already have this event defined, keep it. Otherwise comment it temporarily.
            // _bus.Publish(new CallAssignedDomainEvent(T: tSim, Source: "ElevatorController", CallId: call.CallId, VehicleId: elevatorId, EstimatedPickupT: double.NaN));

            elevator.SetTarget(call.OriginFloor);
        }
    }

    private void StepAssignment(Building building, Elevator elevator, ActiveAssignment a, double tSim)
    {
        var call = a.Call;

        switch (a.Phase)
        {
            case AssignmentPhase.GoingToPickup:
                {
                    // Wait until elevator arrives at pickup floor and doors are open
                    if (elevator.CurrentFloor == call.OriginFloor && elevator.State == FlowCore.Contracts.Common.VehicleState.DoorsOpen)
                    {
                        a.Phase = AssignmentPhase.DoorDwellAtPickup;
                    }
                    break;
                }

            case AssignmentPhase.DoorDwellAtPickup:
                {
                    if (!a.DoorDwellStartedAtPickup)
                    {
                        a.DoorDwellStartedAtPickup = true;
                        elevator.BeginDoorDwell(DoorDwellSeconds);
                    }

                    if (elevator.StateTimeRemainingSeconds <= 0)
                    {
                        a.Phase = AssignmentPhase.Boarding;
                    }
                    break;
                }

            case AssignmentPhase.Boarding:
                {
                    if (!a.HasBoarded)
                    {
                        // Minimal v1: board exactly the requested person (1 pax)
                        if (elevator.IsAtCapacity)
                        {
                            // In v1, if at capacity, we just keep trying next tick.
                            // Later: emit CapacityHit event and requeue.
                            return;
                        }

                        // Remove from floor queue only once
                        if (!a.HasDequeuedFromFloorQueue)
                        {
                            a.HasDequeuedFromFloorQueue = true;

                            var floor = building.GetFloor(call.OriginFloor);

                            // v1 assumption: the person is at the head of the queue.
                            if (call.Direction == 1 && floor.WaitingUpCount > 0) floor.DequeueUp();
                            if (call.Direction == 2 && floor.WaitingDownCount > 0) floor.DequeueDown();
                        }

                        elevator.AddPassenger(call.PersonId);
                        elevator.BeginBoarding(peopleCount: 1);
                        a.HasBoarded = true;

                        // If you already have this event defined, keep it. Otherwise comment it temporarily.
                        // _bus.Publish(new PersonBoardedDomainEvent(T: tSim, Source: $"Elevator#{elevator.Id}", PersonId: call.PersonId, CallId: call.CallId, VehicleId: elevator.Id, Floor: call.OriginFloor, VehicleOccupantCountAfter: elevator.OccupantCount));
                    }

                    if (elevator.StateTimeRemainingSeconds <= 0)
                    {
                        elevator.CloseDoorsToIdle();
                        elevator.SetTarget(call.DestinationFloor);
                        a.Phase = AssignmentPhase.GoingToDropoff;
                    }

                    break;
                }

            case AssignmentPhase.GoingToDropoff:
                {
                    if (elevator.CurrentFloor == call.DestinationFloor && elevator.State == FlowCore.Contracts.Common.VehicleState.DoorsOpen)
                    {
                        a.Phase = AssignmentPhase.DoorDwellAtDropoff;
                    }
                    break;
                }

            case AssignmentPhase.DoorDwellAtDropoff:
                {
                    if (!a.DoorDwellStartedAtDropoff)
                    {
                        a.DoorDwellStartedAtDropoff = true;
                        elevator.BeginDoorDwell(DoorDwellSeconds);
                    }

                    if (elevator.StateTimeRemainingSeconds <= 0)
                    {
                        a.Phase = AssignmentPhase.Unloading;
                    }
                    break;
                }

            case AssignmentPhase.Unloading:
                {
                    if (!a.HasUnloaded)
                    {
                        // Minimal v1: unload the single person.
                        if (elevator.ContainsPassenger(call.PersonId))
                            elevator.RemovePassenger(call.PersonId);

                        elevator.BeginUnloading(peopleCount: 1);
                        a.HasUnloaded = true;

                        // Later: publish PersonAlightedDomainEvent
                    }

                    if (elevator.StateTimeRemainingSeconds <= 0)
                    {
                        elevator.CloseDoorsToIdle();
                        a.Phase = AssignmentPhase.Complete;
                    }

                    break;
                }
        }
    }
}
