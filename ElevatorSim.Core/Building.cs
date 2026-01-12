using System.Collections.Generic;
using ElevatorSim.Interfaces;
using ElevatorSim.DataIO;
using ElevatorSim.Common;


/*
 * How the Building class works
 * The Building class is the top‑level component in the simple elevator simulation. It defines a context for the simulation by holding the total number of floors (NumFloors) and a collection of elevator cars (Elevators). When a Building is constructed, it accepts parameters for the number of floors and the number of elevators. The constructor then creates that many elevator instances, assigning each an ID (starting at 1) and placing them initially at the first floor. These elevators are stored in a list so they can be managed collectively.
 * To control the elevators, the Building instantiates an ElevatorController. This controller is responsible for dispatching elevators to passenger requests. It does so using an elevator dispatch strategy: if a custom strategy is provided to the constructor, the controller uses it; otherwise, it falls back to a default algorithm. By storing the controller in a property (ElevatorController), the simulation can interact with it or query its state.
 * The PassengerController is also created in the constructor. It generates passenger events and uses the building’s RequestElevator method to request elevator service. That method simply forwards the request to the ElevatorController to keep the controllers decoupled; the passenger controller doesn’t need to know the internal workings of elevator dispatch.
 * A Logger property is included for optional logging. It lets the simulation record events (e.g., pickups, drop‑offs) without mixing logging code into the core logic. Since Logger is nullable (DataLogger?), it can be set externally if logging is desired or left null otherwise.
 * Finally, the RequestElevator method is the public entry point for any actor (e.g., PassengerController or other subsystems) to request elevator service. It takes an origin and destination floor, then delegates the request to the elevator controller. This method encapsulates the building’s elevator system and hides the complexity of dispatch logic from callers.
 * Together, these components allow you to simulate the behavior of multiple elevators in a building: passengers call elevators via the building’s request method, the controller selects an elevator based on the dispatch strategy, and each elevator moves toward its target floor and picks up or drops off passengers accordingly.
*/

namespace ElevatorSim.Core
{
    /**
     * @brief Represents the overall building in which the elevators operate.
     *
     * This class models a multi‑storey building for the elevator simulation.
     * It owns the collection of elevator cars, instantiates the elevator and
     * passenger controllers, and exposes a simple method for external actors
     * (e.g. the PassengerController) to request an elevator without knowing
     * controller details.
     */
public class Building
    {
        /** @brief Gets the total number of floors in the building (floors are numbered starting at 1). */
        public int NumFloors { get; }

        /**
         * @brief Gets the collection of elevators installed in this building.
         *
         * Each elevator instance in this list is managed by the ElevatorController.
         */
        public IList<IElevator> Elevators { get; }

        /**
         * @brief Gets the controller responsible for dispatching elevators.
         *
         * The ElevatorController decides which elevator responds to which passenger request
         * using a selected dispatch strategy.
         */
        public ElevatorController ElevatorController { get; }

        /**
         * @brief Gets the controller responsible for creating and managing passengers.
         *
         * The PassengerController acts as an event generator: it instantiates passenger objects
         * and submits elevator requests through this building.
         */
        public PassengerController PassengerController { get; }

        /**
         * @brief Optional logger used to record simulation events.
         *
         * Keeping logging separate from the core simulation allows flexible output targets
         * (such as console or file).  This property may be null if no logging is desired.
         */
        public DataLogger? Logger { get; set; }

        /**
         * @brief Constructs a new building with a specified number of floors and elevators.
         *
         * @param numFloors        Total number of floors in the building.
         * @param numElevators     Number of elevator cars to create.
         * @param dispatchStrategy Optional strategy controlling how elevators are assigned to requests.
         *                         If null, a default strategy is used by ElevatorController.
         *
         * The constructor initializes the Elevators collection, creates each elevator
         * (positioning them initially on the first floor), and instantiates both the
         * ElevatorController and PassengerController.  Elevators are assigned sequential
         * IDs starting from 1.
         */
        public Building(int numFloors, int numElevators, IElevatorDispatchStrategy? dispatchStrategy = null)
        {
            NumFloors = numFloors;
            Elevators = new List<IElevator>();

            // Create the specified number of elevators; each starts at floor 1.
            for (int i = 0; i < numElevators; i++)
            {
                Elevators.Add(new Elevator(id: i + 1, initialFloor: 1));
            }

            // Create the controller responsible for dispatching elevator requests.
            ElevatorController = new ElevatorController((List<IElevator>)Elevators, dispatchStrategy);

            // Create the passenger controller; it will generate passengers and submit requests
            // through this building instance.
            PassengerController = new PassengerController(this);
        }

        /**
         * @brief Submits a passenger request for an elevator.
         *
         * @param originFloor      The floor where the passenger requests pickup.
         * @param destinationFloor The passenger’s desired destination floor.
         *
         * This method simply forwards the request to the ElevatorController.  Providing this
         * method on the Building decouples the PassengerController (and any other actors)
         * from the specific implementation details of ElevatorController, adhering to the
         * principle of encapsulation.
         */
        public void RequestElevator(int originFloor, int destinationFloor)
        {
            ElevatorController.HandleRequest(originFloor, destinationFloor);
        }
    }
}
