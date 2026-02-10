using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Domain.Building;

public sealed class Floor
{
    public Floor(int floorNumber)
    {
        if (floorNumber < 0) throw new ArgumentOutOfRangeException(nameof(floorNumber));
        FloorNumber = floorNumber;
    }

    public int FloorNumber { get; }

    // People currently “on” the floor (Person IDs)
    public List<int> CurrentOccupants { get; } = new();

    // Call queues (Call IDs). Split by direction.
    public Queue<int> WaitingUpCalls { get; } = new();
    public Queue<int> WaitingDownCalls { get; } = new();

    public int MaxQueueObservedUp { get; private set; }
    public int MaxQueueObservedDown { get; private set; }

    public void TrackQueueMaxima()
    {
        MaxQueueObservedUp = Math.Max(MaxQueueObservedUp, WaitingUpCalls.Count);
        MaxQueueObservedDown = Math.Max(MaxQueueObservedDown, WaitingDownCalls.Count);
    }
}

