/*
 * Explanation of the SimulationConfig class
 * SimulationConfig is a simple data container used by the elevator simulation to hold its initial settings. It defines for public properties with getters and setters:
 * NumFloors — the total number of floors in the simulated building. This value drives the range of valid origin and destination floors for passengers and elevators.
 * NumElevators — the number of elevator cars available in the simulation. Controllers use this value to create and manage the elevator objects.
 * ElevatorCapacity — the maximum number of passengers that can be on a single elevator at once. This constraint ensures that boarding logic respects realistic capacity limits.
 * ElevatorCount — a duplicate of NumElevators that may be needed for backward compatibility with existing configuration files or different naming conventions. The presence of this alias means client code should ensure both properties remain consistent if they are both used.
 * Because the class exposes only simple properties, it has no behavior of its own; it acts purely as a way to transfer configuration data from a file or user input into the simulation. Additional properties (for example, passenger arrival rates, simulation time step, or dispatch algorithm selection) can be added as needed without changing the rest of the system.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorSim.DataIO
{
    /**
     * @brief Configuration parameters for the elevator simulation.
     *
     * A SimulationConfig instance bundles together the high‑level settings
     * needed to initialise the simulation: how many floors the building
     * has, how many elevator cars are available, how many people each car
     * can carry, and any additional parameters required by more advanced
     * scenarios.
     */
    public class SimulationConfig
    {
        /** @brief Number of floors in the simulated building. */
        public int NumFloors { get; set; }

        /** @brief Number of elevator cars in the simulation. */
        public int NumElevators { get; set; }

        /** @brief Maximum number of passengers an elevator can hold. */
        public int ElevatorCapacity { get; set; }

        /**
         * @brief Alias for the number of elevators.
         *
         * This property mirrors @c NumElevators and can be used interchangeably.
         * It exists to support legacy configuration fields or different naming
         * conventions in external configuration files.  If both values are
         * present, the caller should take care to keep them consistent.
         */
        public int ElevatorCount { get; set; }

        // Additional configuration fields (e.g. simulation speed, passenger arrival rate)
        // can be added here as needed to extend the simulation model.
    }
}
