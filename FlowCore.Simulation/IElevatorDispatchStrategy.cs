using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowCore.Domain.Requests;

namespace FlowCore.Simulation;

public interface IElevatorDispatchStrategy
{
    int SelectElevator(IReadOnlyList<Elevator> fleet, CallRequest call);
}


