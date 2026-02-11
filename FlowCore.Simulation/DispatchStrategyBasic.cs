using FlowCore.Domain.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using FlowCore.Contracts.Common;

namespace FlowCore.Simulation;

public sealed class DispatchStrategyBasic : IElevatorDispatchStrategy
{
    public int SelectElevator(IReadOnlyList<Elevator> fleet, CallRequest call)
    {
        if (fleet is null || fleet.Count == 0)
            throw new ArgumentException("Fleet is empty.", nameof(fleet));

        var origin = call.OriginFloor;

        var candidate = fleet
            .OrderBy(e => e.State == VehicleState.Idle ? 0 : 1)
            .ThenBy(e => Math.Abs(e.CurrentFloor - origin))
            .ThenBy(e => e.Id)
            .First();

        return candidate.Id;
    }
}



