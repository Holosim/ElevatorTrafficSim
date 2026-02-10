using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowCore.Domain.Building;

namespace FlowCore.Simulation.Adapters;

/// <summary>
/// Narrow read-only surface for snapshot assembly.
/// Keeps the assembler from depending on extra domain details.
/// </summary>
public interface IBuildingReadModel
{
    int FloorCount { get; }
    IReadOnlyList<Floor> Floors { get; }
}

