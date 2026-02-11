using FlowCore.Contracts.Common;
using FlowCore.Domain.Building;
using FlowCore.Domain.Events;
using FlowCore.Domain.Requests;
using FlowCore.Simulation.Events;

namespace FlowCore.Simulation;

public sealed class ElevatorController
{
    // Door timing (seconds)
    private const double DoorOpenSeconds = 1.0;
    private const double DoorCloseSeconds = 1.0;

    // Passenger service timing (seconds)
    private const double BoardSecondsPerPerson = 1.0;
    private const double AlightSecondsPerPerson = 0.5;

    // We model door dwell as the combined open + close overhead at the stop.
    // If you later model explicit open/close phases in the Elevator class, split these out.
    private static readonly double DoorDwellSeconds = DoorOpenSeconds + DoorCloseSeconds;

    private readonly IEventBus _bus;
    private readonly IElevatorDispatchStrategy _strategy;
    private readonly List<Elevator> _fleet;

    private readonly Queue<CallRequest> _pendingCalls = new();
    private readonly Dictionary<int, ActiveAssignment> _active = new();

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

        // Boarded calls captured at pickup. Includes primary + any extra batch-boarded calls.
        public List<CallRequest> BoardedCalls { get; } = new();

        // Tracks whether we already emitted the cooldown notification for "departing pickup".
        public bool CooldownNotifiedOnDeparture { get; set; }

        // When we finish boarding, we move toward this next target.
        public int? NextTargetFloorAfterBoarding { get; set; }
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

            _active[elevatorId] = new ActiveAssignment(call);

            _bus.Publish(new CallAssignedDomainEvent(
                T: tSim,
                Source: "ElevatorController",
                CallId: call.CallId,
                VehicleId: elevatorId,
                EstimatedPickupT: double.NaN));

            var elevator = _fleet.First(e => e.Id == elevatorId);
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
                    // Wait until the elevator arrives and doors open.
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
                    // If we are still in a timed boarding state, wait until it finishes,
                    // then close doors and depart to the next target.
                    if (elevator.State == VehicleState.Loading && elevator.StateTimeRemainingSeconds > 0)
                    {
                        // still boarding time ticking down
                        break;
                    }

                    // If boarding time already ran and we have a next target, depart now.
                    if (elevator.State != VehicleState.Loading &&
                        a.NextTargetFloorAfterBoarding.HasValue &&
                        elevator.State == VehicleState.DoorsOpen)
                    {
                        // Doors are still open at pickup. Close and depart.
                        elevator.CloseDoorsToIdle();

                        // Cooldown is "after the elevator leaves". We notify at the moment we depart pickup.
                        if (!a.CooldownNotifiedOnDeparture &&
                            _strategy is ICooldownAwareDispatchStrategy cool)
                        {
                            cool.NotifyElevatorDeparted(elevator.Id, tSim);
                            a.CooldownNotifiedOnDeparture = true;
                        }

                        elevator.SetTarget(a.NextTargetFloorAfterBoarding.Value);
                        a.Phase = AssignmentPhase.GoingToDropoff;
                        break;
                    }

                    // Otherwise, we have not started boarding yet. Attempt to board a batch.
                    var capacityRemaining = elevator.Capacity - elevator.OccupantCount;
                    if (capacityRemaining <= 0)
                    {
                        // Elevator arrived full. Do NOT lose the request.
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

                    // Collect primary + any other pending calls that match this pickup (same floor + direction).
                    // This does not touch floor queues yet.
                    var boardedCalls = CollectBatchForPickup(call, maxCount: capacityRemaining);

                    if (boardedCalls.Count == 0)
                    {
                        // Nobody to board (shouldn't happen because batch always includes primary),
                        // but we handle defensively.
                        _pendingCalls.Enqueue(call);
                        elevator.CloseDoorsToIdle();
                        a.Phase = AssignmentPhase.Complete;
                        break;
                    }

                    // Now dequeue the corresponding people from the floor queue FIFO.
                    var floor = building.GetFloor(call.OriginFloor);
                    var dir = call.Direction;

                    var actuallyDequeued = 0;
                    for (int i = 0; i < boardedCalls.Count; i++)
                    {
                        if (dir == 1)
                        {
                            if (floor.WaitingUpCount > 0)
                            {
                                floor.DequeueUp();
                                actuallyDequeued++;
                            }
                        }
                        else
                        {
                            if (floor.WaitingDownCount > 0)
                            {
                                floor.DequeueDown();
                                actuallyDequeued++;
                            }
                        }
                    }

                    // Publish updated queue size after dequeue
                    _bus.Publish(new QueueSizeChangedDomainEvent(
                        T: tSim,
                        Source: $"Floor#{call.OriginFloor}",
                        Floor: call.OriginFloor,
                        Direction: dir,
                        NewQueueSize: dir == 1 ? floor.WaitingUpCount : floor.WaitingDownCount));

                    // Board them into elevator (stop if capacity reached unexpectedly).
                    var boardedCount = 0;
                    foreach (var c in boardedCalls)
                    {
                        if (elevator.IsAtCapacity)
                        {
                            // If this happens, the remaining calls must be re-queued.
                            _pendingCalls.Enqueue(c);
                            continue;
                        }

                        elevator.AddPassenger(c.PersonId);
                        a.BoardedCalls.Add(c);
                        boardedCount++;

                        _bus.Publish(new PersonBoardedDomainEvent(
                            T: tSim,
                            Source: $"Elevator#{elevator.Id}",
                            PersonId: c.PersonId,
                            CallId: c.CallId,
                            VehicleId: elevator.Id,
                            Floor: c.OriginFloor,
                            VehicleOccupantCountAfter: elevator.OccupantCount));
                    }

                    if (boardedCount <= 0)
                    {
                        // Could not board anyone. Put primary back and quit.
                        _pendingCalls.Enqueue(call);
                        elevator.CloseDoorsToIdle();
                        a.Phase = AssignmentPhase.Complete;
                        break;
                    }

                    // Apply boarding time cost: 1.0s per boarded passenger
                    elevator.BeginBoarding(boardedCount);

                    // Decide next target among boarded passengers.
                    var nextTarget = a.BoardedCalls
                        .Select(c => c.DestinationFloor)
                        .OrderBy(f => Math.Abs(f - elevator.CurrentFloor))
                        .First();

                    a.NextTargetFloorAfterBoarding = nextTarget;

                    // We remain in Boarding until loading time completes, then depart.
                    break;
                }

            case AssignmentPhase.GoingToDropoff:
                {
                    // Wait until we arrive and doors open at a floor where someone needs to get off.
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
                    // If unloading is in progress, wait.
                    if (elevator.State == VehicleState.Unloading && elevator.StateTimeRemainingSeconds > 0)
                        break;

                    // If unloading already finished and doors are open, decide where to go next.
                    if (elevator.State != VehicleState.Unloading &&
                        elevator.State == VehicleState.DoorsOpen &&
                        a.DoorDwellStartedAtDropoff && a.BoardedCalls.Count >= 0)
                    {
                        // If we already removed passengers and began unloading time on a prior tick,
                        // this branch closes doors and continues.
                        if (elevator.StateTimeRemainingSeconds <= 0 && a.BoardedCalls.Count == 0)
                        {
                            elevator.CloseDoorsToIdle();
                            a.Phase = AssignmentPhase.Complete;
                            break;
                        }
                    }

                    // Start unloading now (remove those whose destination is current floor).
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

                    // Remove completed calls from assignment list
                    foreach (var c in unloading)
                        a.BoardedCalls.Remove(c);

                    // After unloading finishes, either complete or go to next destination
                    if (elevator.State == VehicleState.Unloading && elevator.StateTimeRemainingSeconds > 0)
                        break;

                    if (a.BoardedCalls.Count == 0)
                    {
                        elevator.CloseDoorsToIdle();
                        a.Phase = AssignmentPhase.Complete;
                    }
                    else
                    {
                        var nextTarget = a.BoardedCalls
                            .Select(c => c.DestinationFloor)
                            .OrderBy(f => Math.Abs(f - elevator.CurrentFloor))
                            .First();

                        elevator.CloseDoorsToIdle();
                        elevator.SetTarget(nextTarget);

                        // Continue servicing remaining onboard calls.
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
        // Preserve FIFO order by scanning once and rebuilding the queue.
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

        while (remaining.Count > 0)
            _pendingCalls.Enqueue(remaining.Dequeue());

        return batch;
    }
}
