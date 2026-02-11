using FlowCore.Contracts.Common;
using FlowCore.Domain.Building;
using FlowCore.Domain.Events;
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

    private const double DoorDwellSeconds = 2.0;

    private sealed class ActiveAssignment
    {
        public ActiveAssignment(CallRequest primaryCall)
        {
            PrimaryCall = primaryCall;
            Phase = AssignmentPhase.GoingToPickup;
        }

        public CallRequest PrimaryCall { get; }

        public AssignmentPhase Phase { get; set; }

        public bool DoorDwellStartedAtPickup { get; set; }
        public bool DoorDwellStartedAtDropoff { get; set; }

        // All calls boarded during pickup (includes primary + extra)
        public List<CallRequest> BoardedCalls { get; } = new();
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
        if (_strategy is CooldownDispatchStrategy cds)
            cds.SetSimTime(tSim);

        AssignPendingCalls(tSim);

        foreach (var elevator in _fleet)
        {
            if (!_active.TryGetValue(elevator.Id, out var a))
                continue;

            StepAssignment(building, elevator, a, tSim);

            if (a.Phase == AssignmentPhase.Complete)
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
                break; // selected elevator is busy, stop for now

            _pendingCalls.Dequeue();
            var elevator = _fleet.First(e => e.Id == elevatorId);

            _active[elevatorId] = new ActiveAssignment(call);

            _bus.Publish(new CallAssignedDomainEvent(
                T: tSim,
                Source: "ElevatorController",
                CallId: call.CallId,
                VehicleId: elevatorId,
                EstimatedPickupT: double.NaN));

            elevator.SetTarget(call.OriginFloor);
        }
    }

    private void StepAssignment(Building building, Elevator elevator, ActiveAssignment a, double tSim)
    {
        var call = a.PrimaryCall;

        switch (a.Phase)
        {
            case AssignmentPhase.GoingToPickup:
                {
                    if (elevator.CurrentFloor == call.OriginFloor &&
                        elevator.State == VehicleState.DoorsOpen)
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
                        a.Phase = AssignmentPhase.Boarding;

                    break;
                }

            case AssignmentPhase.Boarding:
                {
                    // Board the primary call + additional matching calls from _pendingCalls, up to capacity.
                    var capacityRemaining = elevator.Capacity - elevator.OccupantCount;
                    if (capacityRemaining <= 0)
                    {
                        // Capacity block at pickup. Do NOT lose the request.
                        _bus.Publish(new VehicleAtCapacityAtPickupDomainEvent(
                            T: tSim,
                            Source: $"Elevator#{elevator.Id}",
                            CallId: call.CallId,
                            PersonId: call.PersonId,
                            VehicleId: elevator.Id,
                            Floor: call.OriginFloor,
                            VehicleOccupantCount: elevator.OccupantCount,
                            VehicleCapacity: elevator.Capacity));

                        _pendingCalls.Enqueue(call);

                        elevator.CloseDoorsToIdle();
                        a.Phase = AssignmentPhase.Complete;
                        break;
                    }


                    var boarded = CollectBatchForPickup(call, maxCount: capacityRemaining);

                    if (boarded.Count == 0)
                    {
                        // Nobody to board, cancel this assignment.
                        a.Phase = AssignmentPhase.Complete;
                        break;
                    }

                    // Dequeue the same number of personIds from the floor queue (FIFO).
                    var floor = building.GetFloor(call.OriginFloor);
                    var dir = call.Direction;

                    for (int i = 0; i < boarded.Count; i++)
                    {
                        // keep floor queue lengths consistent (assumes FIFO aligns with call order)
                        if (dir == 1 && floor.WaitingUpCount > 0) floor.DequeueUp();
                        if (dir == 2 && floor.WaitingDownCount > 0) floor.DequeueDown();
                    }

                    // Publish updated queue size
                    _bus.Publish(new QueueSizeChangedDomainEvent(
                        T: tSim,
                        Source: $"Floor#{call.OriginFloor}",
                        Floor: call.OriginFloor,
                        Direction: dir,
                        NewQueueSize: dir == 1 ? floor.WaitingUpCount : floor.WaitingDownCount));

                    // Board them into elevator
                    foreach (var c in boarded)
                    {
                        if (elevator.IsAtCapacity)
                            break;

                        elevator.AddPassenger(c.PersonId);
                        a.BoardedCalls.Add(c);

                        _bus.Publish(new PersonBoardedDomainEvent(
                            T: tSim,
                            Source: $"Elevator#{elevator.Id}",
                            PersonId: c.PersonId,
                            CallId: c.CallId,
                            VehicleId: elevator.Id,
                            Floor: c.OriginFloor,
                            VehicleOccupantCountAfter: elevator.OccupantCount));
                    }

                    // Apply boarding time cost: 1.0s per boarded passenger
                    elevator.BeginBoarding(a.BoardedCalls.Count);

                    // Choose next target: nearest destination among boarded calls
                    var nextTarget = a.BoardedCalls
                        .Select(c => c.DestinationFloor)
                        .OrderBy(f => Math.Abs(f - elevator.CurrentFloor))
                        .First();

                    if (_strategy is ICooldownAwareDispatchStrategy cool)
                        cool.NotifyElevatorDeparted(elevator.Id, tSim);

                    // After boarding completes, we will start moving to dropoff
                    a.Phase = AssignmentPhase.GoingToDropoff;

                    // If boarding is instantaneous (unlikely), go immediately
                    if (elevator.StateTimeRemainingSeconds <= 0)
                    {
                        elevator.CloseDoorsToIdle();
                        elevator.SetTarget(nextTarget);
                    }
                    else
                    {
                        // Controller will see Loading complete on a later tick. For now, set target after load finishes.
                        // We store the target by temporarily putting it into elevator.TargetFloor after doors close.
                        // Simplest: set it now. Elevator.Update ignores targets while Loading.
                        elevator.SetTarget(nextTarget);
                    }

                    break;
                }

            case AssignmentPhase.GoingToDropoff:
                {
                    // Wait until we arrive and doors open
                    // Note: Elevator.Update sets DoorsOpen upon arrival.
                    if (elevator.State == VehicleState.DoorsOpen &&
                        a.BoardedCalls.Any(c => c.DestinationFloor == elevator.CurrentFloor))
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
                        a.Phase = AssignmentPhase.Unloading;

                    break;
                }

            case AssignmentPhase.Unloading:
                {
                    // Minimal v1: unload everyone whose destination is the current floor.
                    var unloading = a.BoardedCalls
                        .Where(c => c.DestinationFloor == elevator.CurrentFloor)
                        .ToList();

                    foreach (var c in unloading)
                    {
                        elevator.RemovePassenger(c.PersonId);

                        _bus.Publish(new PersonAlightedDomainEvent(
                            T: tSim,
                            Source: $"Elevator#{elevator.Id}",
                            PersonId: c.PersonId,
                            CallId: c.CallId,
                            VehicleId: elevator.Id,
                            Floor: c.DestinationFloor,
                            VehicleOccupantCountAfter: elevator.OccupantCount));
                    }


                    // Apply unloading time cost: 0.5s per unloaded passenger
                    elevator.BeginUnloading(unloading.Count);

                    // Remove completed calls from this assignment list
                    foreach (var c in unloading)
                        a.BoardedCalls.Remove(c);

                    // Decide what to do next after unloading completes
                    if (a.BoardedCalls.Count == 0)
                    {
                        // Assignment done
                        a.Phase = AssignmentPhase.Complete;
                        elevator.CloseDoorsToIdle();
                    }
                    else
                    {
                        // Continue to next nearest destination among remaining passengers
                        var nextTarget = a.BoardedCalls
                            .Select(c => c.DestinationFloor)
                            .OrderBy(f => Math.Abs(f - elevator.CurrentFloor))
                            .First();

                        elevator.CloseDoorsToIdle();
                        elevator.SetTarget(nextTarget);

                        // Go back to dropoff phase (same logic)
                        a.DoorDwellStartedAtDropoff = false;
                        a.Phase = AssignmentPhase.GoingToDropoff;
                    }

                    break;
                }
        }
    }

    private List<CallRequest> CollectBatchForPickup(CallRequest primary, int maxCount)
    {
        // Always include primary first
        var batch = new List<CallRequest>(capacity: Math.Min(maxCount, 16)) { primary };

        if (maxCount <= 1)
            return batch;

        // Pull additional calls from _pendingCalls that match origin + direction.
        // We preserve FIFO order by scanning once and rebuilding the queue.
        var remaining = new Queue<CallRequest>(_pendingCalls.Count);
        while (_pendingCalls.Count > 0)
        {
            var c = _pendingCalls.Dequeue();

            var matches =
                c.OriginFloor == primary.OriginFloor &&
                c.Direction == primary.Direction;

            if (matches && batch.Count < maxCount)
            {
                batch.Add(c);
            }
            else
            {
                remaining.Enqueue(c);
            }
        }

        // Restore pending queue
        while (remaining.Count > 0)
            _pendingCalls.Enqueue(remaining.Dequeue());

        return batch;
    }
}
