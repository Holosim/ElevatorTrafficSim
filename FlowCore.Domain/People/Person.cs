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
    public required int Id { get; init; }
    public required PassengerType Type { get; init; }
    public required Route Route { get; init; }

    public int CurrentFloor { get; set; }
    public int RouteIndex { get; set; } = 0;
    public PersonState State { get; set; } = PersonState.NotSpawned;
}
