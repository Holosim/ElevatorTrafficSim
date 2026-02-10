using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowCore.Domain.Common;
using FlowCore.Domain.Routing;

namespace FlowCore.Domain.People;

public sealed class Person
{
    public Person(int id, PassengerType type, int startFloor, Route route)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (route is null) throw new ArgumentNullException(nameof(route));

        Id = id;
        Type = type;
        CurrentFloor = startFloor;
        Route = route;

        State = PersonState.NotSpawned;
    }

    public int Id { get; }
    public PassengerType Type { get; }
    public Route Route { get; }

    public PersonState State { get; set; }

    public int CurrentFloor { get; set; }

    // Trip bookkeeping. Minimal. Use events/journals for full history.
    public int? ActiveCallId { get; set; }
}

