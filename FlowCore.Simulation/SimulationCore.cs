using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowCore.Domain.Building;

namespace FlowCore.Simulation;

public sealed class SimulationCore
{
    public void Run(
        Building building,
        ElevatorController elevatorController,
        double durationSimSeconds,
        double dtSimSeconds,
        double speedFloorsPerSecond)
    {
        if (building is null) throw new ArgumentNullException(nameof(building));
        if (elevatorController is null) throw new ArgumentNullException(nameof(elevatorController));
        if (durationSimSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(durationSimSeconds));
        if (dtSimSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(dtSimSeconds));
        if (speedFloorsPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(speedFloorsPerSecond));

        var ticks = (int)Math.Ceiling(durationSimSeconds / dtSimSeconds);
        var tSim = 0.0;

        for (int i = 0; i < ticks; i++)
        {
            // 1) Move vehicles
            foreach (var e in elevatorController.Fleet)
                e.Update(dtSimSeconds, speedFloorsPerSecond);

            // 2) Run controller logic (assign calls, board/unboard, etc.)
            elevatorController.Update(building, tSim, dtSimSeconds);

            tSim += dtSimSeconds;
        }
    }
}
