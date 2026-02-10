using System.Collections.Generic;

using FlowCore.Domain.Building;
using FlowCore.Domain.Common;
using FlowCore.Domain.Events;
using FlowCore.Domain.People;
using FlowCore.Domain.Requests;
using FlowCore.Domain.Routing;

using FlowCore.Simulation.Events;

namespace FlowCore.Simulation;

public sealed class PassengerController
{
    private readonly Random _rng;
    private readonly NonHomogeneousPoisson _nhpp;

    private readonly Dictionary<PassengerType, IArrivalRateCurve> _curves;

    private int _nextPersonId = 1;
    private int _nextCallId = 1;

    // Generated people for this trial (for later analytics)
    private readonly List<Person> _people = new();

    // Scheduled calls (primarily return-to-lobby)
    private readonly PriorityQueue<ScheduledCall, double> _scheduled = new();

    // Next arrival time per type
    private readonly Dictionary<PassengerType, double> _nextArrivalT = new();

    public PassengerController(int seed, IDictionary<PassengerType, IArrivalRateCurve>? curves = null)
    {
        _rng = new Random(seed);
        _nhpp = new NonHomogeneousPoisson(_rng);

        _curves = curves is null
            ? new Dictionary<PassengerType, IArrivalRateCurve>
            {
                { PassengerType.Resident, DefaultArrivalCurves.For(PassengerType.Resident) },
                { PassengerType.OfficeWorker, DefaultArrivalCurves.For(PassengerType.OfficeWorker) },
                { PassengerType.Shopper, DefaultArrivalCurves.For(PassengerType.Shopper) },
            }
            : new Dictionary<PassengerType, IArrivalRateCurve>(curves);

        foreach (var kvp in _curves)
            _nextArrivalT[kvp.Key] = 0.0; // will be initialized on first Update
    }

    public IReadOnlyList<Person> People => _people;

    public (int spawned, int callsSubmitted) Update(
        Building building,
        IEventBus bus,
        ElevatorController elevatorController,
        double tSimSeconds,
        double horizonSeconds = 60.0,
        Action<CallRequest>? onCallSubmitted = null)
    {
        if (building is null) throw new ArgumentNullException(nameof(building));
        if (bus is null) throw new ArgumentNullException(nameof(bus));
        if (elevatorController is null) throw new ArgumentNullException(nameof(elevatorController));

        var spawned = 0;
        var callsSubmitted = 0;

        // 1) Trigger due scheduled calls (return trips, etc.)
        while (_scheduled.Count > 0 && _scheduled.TryPeek(out var sc, out var tDue) && tDue <= tSimSeconds)
        {
            _scheduled.Dequeue();
            elevatorController.SubmitCall(sc.Call);
            onCallSubmitted?.Invoke(sc.Call);
            callsSubmitted++;
            EnqueueFloor(building, bus, sc.Call, tSimSeconds);
        }

        // 2) Spawn new arrivals per passenger type up to current time.
        foreach (var (ptype, curve) in _curves)
        {
            // Initialize next arrival if needed
            var next = _nextArrivalT[ptype];
            if (next <= 0.0 || double.IsPositiveInfinity(next) || next < tSimSeconds)
                _nextArrivalT[ptype] = _nhpp.NextArrivalTime(tSimSeconds, curve, horizonSeconds);
            var nextT = _nhpp.NextArrivalTime(tSimSeconds, curve, horizonSeconds);
            if (double.IsPositiveInfinity(nextT))
                nextT = tSimSeconds + horizonSeconds; // try again next window
            _nextArrivalT[ptype] = nextT;


            // Spawn all arrivals that happen at or before now
            while (_nextArrivalT[ptype] <= tSimSeconds)
            {
                var person = SpawnPerson(building, ptype);
                _people.Add(person);
                spawned++;

                // First call: lobby -> destination
                var dest = person.Route.Destinations[0];
                var callUp = new CallRequest(
                    CallId: _nextCallId++,
                    PersonId: person.Id,
                    PersonType: person.Type,
                    OriginFloor: 0,
                    DestinationFloor: dest.Floor,
                    Direction: dest.Floor > 0 ? 1 : 2,
                    RequestT: tSimSeconds);

                elevatorController.SubmitCall(callUp);
                onCallSubmitted?.Invoke(callUp);
                callsSubmitted++;
                EnqueueFloor(building, bus, callUp, tSimSeconds);

                // Schedule return-to-lobby after planned stay (dest -> 0)
                // (We ignore ride time for now. We’ll refine later with event-driven actual arrival.)
                var returnT = tSimSeconds + Math.Max(0, dest.PlannedStaySeconds);

                var callDown = new CallRequest(
                    CallId: _nextCallId++,
                    PersonId: person.Id,
                    PersonType: person.Type,
                    OriginFloor: dest.Floor,
                    DestinationFloor: 0,
                    Direction: dest.Floor > 0 ? 2 : 1,
                    RequestT: returnT);

                _scheduled.Enqueue(new ScheduledCall(callDown), returnT);

                // Compute next arrival time for this type
                _nextArrivalT[ptype] = nextT;

                // If curve is zero for rest of horizon, bail
                if (double.IsPositiveInfinity(nextT))
                    break;
            }
        }

        return (spawned, callsSubmitted);
    }

    private Person SpawnPerson(Building building, PassengerType type)
    {
        // v1: all arrivals begin in lobby (0)
        // Choose destination based on type and building rules you described
        var destFloor = type switch
        {
            PassengerType.Shopper => SampleInt(1, Math.Min(5, building.FloorCount - 1)),
            PassengerType.OfficeWorker => SampleInt(6, Math.Min(20, building.FloorCount - 1)),
            PassengerType.Resident => SampleInt(21, Math.Min(39, building.FloorCount - 1)),
            _ => SampleInt(1, building.FloorCount - 1)
        };

        var staySeconds = type switch
        {
            PassengerType.Shopper => SampleRangeSeconds(min: 900, max: 5400),        // 15–90 min
            PassengerType.OfficeWorker => SampleRangeSeconds(min: 8 * 3600, max: 10 * 3600),
            PassengerType.Resident => SampleRangeSeconds(min: 1800, max: 4 * 3600), // 30–240 min
            _ => 1800
        };

        var route = new Route(new[]
        {
            new Destination(destFloor, staySeconds)
        });

        return new Person(
            id: _nextPersonId++,
            type: type,
            startFloor: 0,
            route: route);
    }

    private void EnqueueFloor(Building building, IEventBus bus, CallRequest call, double tSimSeconds)
    {
        var floor = building.GetFloor(call.OriginFloor);

        if (call.Direction == 1)
        {
            floor.EnqueueUp(call.PersonId);
            bus.Publish(new QueueSizeChangedDomainEvent(
                T: tSimSeconds,
                Source: $"Floor#{call.OriginFloor}",
                Floor: call.OriginFloor,
                Direction: 1,
                NewQueueSize: floor.WaitingUpCount));
        }
        else
        {
            floor.EnqueueDown(call.PersonId);
            bus.Publish(new QueueSizeChangedDomainEvent(
                T: tSimSeconds,
                Source: $"Floor#{call.OriginFloor}",
                Floor: call.OriginFloor,
                Direction: 2,
                NewQueueSize: floor.WaitingDownCount));
        }
    }

    private int SampleInt(int minInclusive, int maxInclusive)
    {
        if (maxInclusive < minInclusive) return minInclusive;
        return _rng.Next(minInclusive, maxInclusive + 1);
    }

    private double SampleRangeSeconds(double min, double max)
    {
        if (max <= min) return min;
        return min + (_rng.NextDouble() * (max - min));
    }

    private readonly record struct ScheduledCall(CallRequest Call);
}
