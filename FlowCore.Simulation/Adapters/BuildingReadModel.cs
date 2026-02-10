using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowCore.Domain.Building;

namespace FlowCore.Simulation.Adapters;

public sealed class BuildingReadModel : IBuildingReadModel
{
    private readonly Building _building;

    public BuildingReadModel(Building building) => _building = building;

    public int FloorCount => _building.FloorCount;
    public IReadOnlyList<Floor> Floors => _building.Floors;
}
