using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Simulation
{
    public interface ICooldownAwareDispatchStrategy
    {
        void NotifyElevatorDeparted(int elevatorId, double tSim);
    }

}
