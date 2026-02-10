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
        if (floorCount <= 1) throw new ArgumentOutOfRangeException(nameof(floorCount));

        _floors = new List<Floor>(capacity: floorCount);
        for (int i = 0; i < floorCount; i++)
            _floors.Add(new Floor(i));
    }

    public int FloorCount => _floors.Count;

    public IReadOnlyList<Floor> Floors => _floors;

    public Floor GetFloor(int floorNumber)
    {
        if (floorNumber < 0 || floorNumber >= _floors.Count)
            throw new ArgumentOutOfRangeException(nameof(floorNumber));

        return _floors[floorNumber];
    }
}


