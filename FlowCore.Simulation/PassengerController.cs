using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowCore.Domain.Building;
using FlowCore.Domain.People;
using FlowCore.Domain.Requests;
using FlowCore.Simulation.Events;

namespace FlowCore.Simulation;

public sealed class PassengerController
{
    private readonly IEventBus _bus;
    private int _nextPersonId = 1;
    private int _nextCallId = 1;

    public PassengerController(IEventBus bus)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
    }

    public List<Person> Occupants { get; } = new();

    public Person SpawnPerson(Person person)
    {
        Occupants.Add(person);
        return person;
    }

    public CallRequest CreateCall(int personId, int originFloor, int destinationFloor, int direction, double tSim)
    {
        // Placeholder: caller passes type in a real implementation, or we look it up.
        var person = Occupants.First(p => p.Id == personId);

        return new CallRequest(
            CallId: _nextCallId++,
            PersonId: personId,
            PersonType: person.Type,
            OriginFloor: originFloor,
            DestinationFloor: destinationFloor,
            Direction: direction,
            RequestT: tSim
        );
    }

    public void Update(Building building, double tSim, double dtSimSeconds)
    {
        // Later: arrival scheduler and “stay duration” transitions.
    }
}

