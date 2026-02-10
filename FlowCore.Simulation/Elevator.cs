using FlowCore.Contracts.Common;

namespace FlowCore.Simulation;

public sealed class Elevator
{
    private readonly List<int> _passengers = new();
    private readonly List<int> _stopQueueFloors = new();

    private double _stateTimeRemainingSeconds;

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

        _stateTimeRemainingSeconds = 0;
    }

    public int Id { get; }
    public int Capacity { get; }

    public double PositionFloor { get; private set; }

    // A stable floor index for “arrived at floor” logic
    public int CurrentFloor => (int)Math.Round(PositionFloor, MidpointRounding.AwayFromZero);

    public int? TargetFloor { get; private set; }

    public MotionDirection Direction { get; private set; }
    public VehicleState State { get; private set; }

    public int OccupantCount => _passengers.Count;

    public IReadOnlyList<int> StopQueueFloors => _stopQueueFloors;

    public double StateTimeRemainingSeconds => _stateTimeRemainingSeconds;

    public bool IsAtCapacity => _passengers.Count >= Capacity;

    public bool ContainsPassenger(int personId) => _passengers.Contains(personId);

    public void AddPassenger(int personId)
    {
        if (IsAtCapacity) throw new InvalidOperationException("Elevator is at capacity.");
        _passengers.Add(personId);
    }

    public bool RemovePassenger(int personId) => _passengers.Remove(personId);

    /// <summary>
    /// Controller sets a new motion target. If already at that floor, elevator enters DoorsOpen state immediately.
    /// </summary>
    public void SetTarget(int floor)
    {
        TargetFloor = floor;

        if (floor == CurrentFloor)
        {
            Direction = MotionDirection.Idle;
            State = VehicleState.DoorsOpen;
            _stateTimeRemainingSeconds = 0;
            return;
        }

        Direction = floor > CurrentFloor ? MotionDirection.Up : MotionDirection.Down;
        State = VehicleState.Moving;
        _stateTimeRemainingSeconds = 0;
    }

    public void BeginDoorDwell(double dwellSeconds)
    {
        State = VehicleState.DoorsOpen;
        Direction = MotionDirection.Idle;
        _stateTimeRemainingSeconds = Math.Max(0, dwellSeconds);
    }

    public void BeginBoarding(int peopleCount)
    {
        // 1.0 second per person
        State = VehicleState.Loading;
        Direction = MotionDirection.Idle;
        _stateTimeRemainingSeconds = Math.Max(0, peopleCount) * 1.0;
    }

    public void BeginUnloading(int peopleCount)
    {
        // 0.5 seconds per person
        State = VehicleState.Unloading;
        Direction = MotionDirection.Idle;
        _stateTimeRemainingSeconds = Math.Max(0, peopleCount) * 0.5;
    }

    public void CloseDoorsToIdle()
    {
        State = VehicleState.Idle;
        Direction = MotionDirection.Idle;
        _stateTimeRemainingSeconds = 0;
    }

    /// <summary>
    /// Advances mechanics by dtSim. Controller handles call lifecycle decisions.
    /// </summary>
    public void Update(double dtSimSeconds, double speedFloorsPerSecond)
    {
        if (dtSimSeconds <= 0) return;

        // Timed states: DoorsOpen dwell, Loading, Unloading.
        if (State is VehicleState.DoorsOpen or VehicleState.Loading or VehicleState.Unloading)
        {
            _stateTimeRemainingSeconds -= dtSimSeconds;
            if (_stateTimeRemainingSeconds < 0)
                _stateTimeRemainingSeconds = 0;

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

        // Arrival check
        if (Math.Abs(PositionFloor - target) < 1e-6)
        {
            PositionFloor = target;
            Direction = MotionDirection.Idle;
            State = VehicleState.DoorsOpen;
            _stateTimeRemainingSeconds = 0;
        }
    }
}
