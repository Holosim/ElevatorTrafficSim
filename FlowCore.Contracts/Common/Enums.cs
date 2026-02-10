using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Contracts.Common;

public enum PassengerType
{
    Resident,
    OfficeWorker,
    Shopper
}

public enum PersonState
{
    NotSpawned,
    Waiting,
    Riding,
    Staying,
    Completed
}

public enum MotionDirection
{
    Idle,
    Up,
    Down
}

public enum VehicleState
{
    Idle,
    Moving,
    DoorsOpen,
    Loading,
    Unloading,
    OutOfService
}
