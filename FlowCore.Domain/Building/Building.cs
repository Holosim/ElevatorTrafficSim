using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Domain.Building;

public sealed class Building
{
    private readonly List<Floor> _floors;

    public Building(int floorCount)
    {
        if (floorCount <= 0) throw new ArgumentOutOfRangeException(nameof(floorCount));
        _floors = Enumerable.Range(0, floorCount).Select(i => new Floor(i)).ToList();
    }

    public int FloorCount => _floors.Count;
    public IReadOnlyList<Floor> Floors => _floors;

    public Floor GetFloor(int floorNumber) => _floors[floorNumber];
}

