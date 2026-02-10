using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Domain.Building;

public sealed class Floor
{
    private readonly Queue<int> _waitingUp = new();
    private readonly Queue<int> _waitingDown = new();

    public Floor(int floorNumber)
    {
        FloorNumber = floorNumber;
    }

    public int FloorNumber { get; }

    // “Current occupants” can be a count for now. Later it can be a set/list.
    public int CurrentOccupantsCount { get; set; }

    public IReadOnlyCollection<int> WaitingUpCalls => _waitingUp;
    public IReadOnlyCollection<int> WaitingDownCalls => _waitingDown;

    public void EnqueueUp(int personId) => _waitingUp.Enqueue(personId);
    public void EnqueueDown(int personId) => _waitingDown.Enqueue(personId);

    public int DequeueUp() => _waitingUp.Dequeue();
    public int DequeueDown() => _waitingDown.Dequeue();

    public int WaitingUpCount => _waitingUp.Count;
    public int WaitingDownCount => _waitingDown.Count;
}


