using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Domain.Events;

public interface IDomainEvent
{
    double T { get; }
    string Source { get; }
}

public readonly record struct CallAssignedDomainEvent(
    double T,
    string Source,
    int CallId,
    int VehicleId,
    double EstimatedPickupT
) : IDomainEvent;

public readonly record struct PersonBoardedDomainEvent(
    double T,
    string Source,
    int PersonId,
    int CallId,
    int VehicleId,
    int Floor,
    int VehicleOccupantCountAfter
) : IDomainEvent;

public readonly record struct QueueSizeChangedDomainEvent(
    double T,
    string Source,
    int Floor,
    int Direction,      // 0 idle, 1 up, 2 down. Or use an enum in Domain.
    int NewQueueSize
) : IDomainEvent;

