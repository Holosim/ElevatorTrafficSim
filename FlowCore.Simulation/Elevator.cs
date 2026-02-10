using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowCore.Contracts.Common;

namespace FlowCore.Simulation;

public sealed class Elevator
{
    private readonly List<int> _passengers = new();
    private readonly List<int> _stopQueueFloors = new();

    // Timing state (seconds remaining in current sub-state)
    private double _stateTimeRemaining;

    public Elevator(int id, int capacity, int startFloor = 0)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

        Id = id;
        Capacity = capacity;

        PositionFloor = startFloor;
        TargetFloor = null;

        Direction = MotionDirection.Idle;
        State = VehicleState.Idle;
        _stateTimeRemaining = 0;
    }

    public int Id { get; }
    public int Capacity { get; }

    public double PositionFloor { get; private set; }
    public int CurrentFloor => (int)Math.Round(PositionFloor, MidpointRounding.AwayFromZero);

    public int? TargetFloor { get; private set; }

    public MotionDirection Direction { get; private set; }
    public VehicleState State { get; private set; }

    public int OccupantCount => _passengers.Count;
    public IReadOnlyList<int> StopQueueFloors => _stopQueueFloors;

    // --- New: expose door/load timing for snapshots/debugging if needed ---
    public double StateTimeRemaining => _stateTimeRemaining;

    // --- Minimal API for controller to schedule work ---
    public void AssignTarget(int floor)
    {
        TargetFloor = floor;

        if (floor == CurrentFloor)
        {
            // Already here: go directly to doors open (controller can decide).
            State = VehicleState.DoorsOpen;
            Direction = MotionDirection.Idle;
            _stateTimeRemaining = 0;
            return;
        }

        State = VehicleState.Moving;
        Direction = floor > CurrentFloor ? MotionDirection.Up : MotionDirection.Down;
        _stateTimeRemaining = 0;
    }

    /// <summary>
    /// Called when elevator should begin a "doors open dwell".
    /// </summary>
    public void BeginDoorsOpen(double dwellSeconds)
    {
        State = VehicleState.DoorsOpen;
        Direction = MotionDirection.Idle;
        _stateTimeRemaining = Math.Max(0, dwellSeconds);
    }

    /// <summary>
    /// Called to begin boarding for N people. 1.0s per person.
    /// </summary>
    public void BeginBoarding(int peopleCount)
    {
        State = VehicleState.Loading;
        Direction = MotionDirection.Idle;
        _stateTimeRemaining = Math.Max(0, peopleCount) * 1.0;
    }

    /// <summary>
    /// Called to begin unloading for N people. 0.5s per person.
    /// </summary>
    public void BeginUnloading(int peopleCount)
    {
        State = VehicleState.Unloading;
        Direction = MotionDirection.Idle;
        _stateTimeRemaining = Math.Max(0, peopleCount) * 0.5;
    }

    public void CloseDoors()
    {
        State = VehicleState.Idle;
        Direction = MotionDirection.Idle;
        _stateTimeRemaining = 0;
    }


    public void AddPassenger(int personId)
    {
        if (_passengers.Count >= Capacity) throw new InvalidOperationException("Elevator at capacity.");
        _passengers.Add(personId);
    }

    public bool RemovePassenger(int personId) => _passengers.Remove(personId);

    public bool ContainsPassenger(int personId) => _passengers.Contains(personId);

    public void Update(double dtSimSeconds, double speedFloorsPerSecond)
    {
        if (dtSimSeconds <= 0) return;

        // Timed states. DoorsOpen, Loading, Unloading, DoorsClosed (instant)
        if (State is VehicleState.DoorsOpen or VehicleState.Loading or VehicleState.Unloading)
        {
            _stateTimeRemaining -= dtSimSeconds;
            if (_stateTimeRemaining <= 0)
            {
                _stateTimeRemaining = 0;
                // Controller decides next transition. We simply mark state as DoorsOpenComplete
                // by using DoorsOpen with timeRemaining==0, or set DoorsClosed.
                // Keep it simple: do nothing here.
            }
            return;
        }

        if (State != VehicleState.Moving || TargetFloor is null) return;

        var target = TargetFloor.Value;
        var deltaFloors = speedFloorsPerSecond * dtSimSeconds;

        if (PositionFloor < target)
        {
            Direction = MotionDirection.Up;
            PositionFloor = Math.Min(target, PositionFloor + deltaFloors);
        }
        else if (PositionFloor > target)
        {
            Direction = MotionDirection.Down;
            PositionFloor = Math.Max(target, PositionFloor - deltaFloors);
        }

        if (Math.Abs(PositionFloor - target) < 1e-6)
        {
            // Arrived. Controller will choose door operations.
            State = VehicleState.DoorsOpen;
            Direction = MotionDirection.Idle;
            _stateTimeRemaining = 0;
        }
    }
}
