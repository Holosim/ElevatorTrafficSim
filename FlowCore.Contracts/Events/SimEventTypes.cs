using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Contracts.Events;

public enum SimEventType
{
    RunStarted,
    RunEnded,

    PersonSpawned,
    PersonStateChanged,

    CallRequested,
    CallAssigned,

    ElevatorArrived,
    DoorsOpened,
    DoorsClosed,

    PersonBoarded,
    PersonAlighted,

    CapacityHit,
    VehicleStateChanged,

    QueueSizeChanged
}

