using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Contracts.Snapshots;

public readonly record struct SimTickSnapshot(
    int RunId,
    long Tick,
    double T,
    IReadOnlyList<ElevatorSnapshot> Elevators,
    IReadOnlyList<FloorQueueSnapshot> Floors
);
