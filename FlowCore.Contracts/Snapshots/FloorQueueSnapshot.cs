using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Contracts.Snapshots;

public readonly record struct FloorQueueSnapshot(
    int Floor,
    int WaitingUp,
    int WaitingDown,
    int CurrentOccupantsOnFloor
);
