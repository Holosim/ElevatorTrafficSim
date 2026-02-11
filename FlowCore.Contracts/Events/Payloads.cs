using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowCore.Contracts.Common;

namespace FlowCore.Contracts.Events;

public readonly record struct RunStartedPayload(
    int FloorCount,
    int ElevatorCount,
    int RandomSeed,
    double PlannedDurationSeconds,
    string ScenarioName,
    string ContractVersion
);

public readonly record struct RunEndedPayload(
    int TotalPeople,
    int TotalCallsCompleted
);

public readonly record struct PersonSpawnedPayload(
    int PersonId,
    PassengerType Type,
    int StartFloor
);

public readonly record struct PersonStateChangedPayload(
    int PersonId,
    PersonState From,
    PersonState To,
    int Floor
);

public readonly record struct CallRequestedPayload(
    int CallId,
    int PersonId,
    PassengerType PersonType,
    int OriginFloor,
    int DestinationFloor
);

public readonly record struct CallAssignedPayload(
    int CallId,
    int VehicleId,
    double EstimatedPickupT
);

public readonly record struct ElevatorArrivedPayload(
    int VehicleId,
    int Floor,
    MotionDirection Direction
);

public readonly record struct DoorsPayload(
    int VehicleId,
    int Floor
);

public readonly record struct PersonBoardedPayload(
    int PersonId,
    int CallId,
    int VehicleId,
    int Floor,
    int VehicleOccupantCountAfter
);

public readonly record struct PersonAlightedPayload(
    int PersonId,
    int CallId,
    int VehicleId,
    int Floor,
    int VehicleOccupantCountAfter
);

public readonly record struct CapacityHitPayload(
    int VehicleId,
    int Floor,
    int Capacity,
    int OccupantCount
);

public readonly record struct VehicleStateChangedPayload(
    int VehicleId,
    VehicleState From,
    VehicleState To,
    int Floor,
    MotionDirection Direction
);

public readonly record struct QueueSizeChangedPayload(
    int Floor,
    MotionDirection Direction,
    int NewQueueSize
);

public readonly record struct VehicleAtCapacityAtPickupPayload(
    int CallId,
    int PersonId,
    int VehicleId,
    int Floor,
    int VehicleOccupantCount,
    int VehicleCapacity
);
