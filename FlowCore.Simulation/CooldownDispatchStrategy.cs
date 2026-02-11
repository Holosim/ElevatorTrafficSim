using FlowCore.Domain.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Simulation;

public sealed class CooldownDispatchStrategy : IElevatorDispatchStrategy, ICooldownAwareDispatchStrategy
{
    private readonly IElevatorDispatchStrategy _inner;
    private readonly double _cooldownSeconds;

    private readonly Dictionary<int, double> _cooldownUntil = new();

    public CooldownDispatchStrategy(IElevatorDispatchStrategy inner, double cooldownSeconds = 3.0)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        if (cooldownSeconds < 0) throw new ArgumentOutOfRangeException(nameof(cooldownSeconds));
        _cooldownSeconds = cooldownSeconds;
    }

    public int SelectElevator(IReadOnlyList<Elevator> fleet, CallRequest call)
    {
        // Filter out elevators still cooling down.
        // If all are cooling down, we fall back to inner anyway (no starvation).
        var eligible = fleet.Where(e => !IsCoolingDown(e.Id)).ToList();
        if (eligible.Count == 0)
            eligible = fleet.ToList();

        return _inner.SelectElevator(eligible, call);
    }

    public void NotifyElevatorDeparted(int elevatorId, double tSim)
    {
        _cooldownUntil[elevatorId] = tSim + _cooldownSeconds;
    }

    private bool IsCoolingDown(int elevatorId)
        => _cooldownUntil.TryGetValue(elevatorId, out var until) && until > 0 && until > _lastSeenSimTime;

    private double _lastSeenSimTime;

    // We need sim time to evaluate cooldown. The simplest is to update this from controller.
    public void SetSimTime(double tSim) => _lastSeenSimTime = tSim;
}
