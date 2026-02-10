using System;
using ElevatorSim.DataIO;

/*Detailed explanation of the class
 * The PassengerController is a lightweight component whose job is to represent the people in the elevator simulation. 
 * It interacts closely with the Building and its ElevatorController to add new passengers and initiate elevator requests:
 * 
 * Dependencies and constructor:    The class holds a single private field, building, which references the Building object representing the simulation environment. 
 *                                  The constructor takes a Building parameter and guards against null by throwing ArgumentNullException if the caller passes null. 
 *                                  This ensures that every PassengerController has a valid context for elevator requests.
 * 
 * Creating passengers: The method CreateAndRequestPassenger takes an origin floor and a destination floor as arguments. 
 *                      It instantiates a new Person object (not shown here but defined elsewhere in the simulation) using those floors. 
 *                      Immediately after creation, it calls building.RequestElevator(originFloor, destinationFloor). 
 *                      That call forwards the request to the elevator controller, which will select an elevator to travel to the origin floor and eventually move the passenger to the destination.
 * 
 * Simplification and extensibility:    As noted in the comments, this controller currently assumes that passengers appear instantaneously and request an elevator right away. 
 *                                      A more sophisticated implementation might simulate arrival patterns or dwell times (for instance, passengers arriving in random intervals or groups). 
 *                                      The design leaves room for such future enhancements without changing the overall contract: the controller generates passengers and triggers elevator requests.
 * 
 * By centralizing passenger creation and elevator requests in one component, the simulation can later add features like different passenger types, arrival schedules, or statistical tracking of wait times in a clean, encapsulated way.
 */

namespace ElevatorSim.Core
{
    /**
     * @brief Generates and manages passengers in the simulation.
     *
     * The PassengerController is responsible for creating @ref Person instances
     * (representing building occupants) and requesting elevators on their behalf.
     * Future enhancements may implement arrival patterns (random or scheduled)
     * and dwell times based on configuration.
     */
public class PassengerController
    {
        /** @brief Reference to the building in which passengers move. */
        private readonly Building building;

        /**
         * @brief Constructs a new PassengerController for a specific building.
         *
         * @param building The building context used to coordinate elevator requests.
         *
         * The building parameter must not be null, otherwise an ArgumentNullException
         * is thrown.  The controller uses this context to route passenger requests
         * to the building’s elevator system.
         */
        public PassengerController(Building building)
        {
            this.building = building ?? throw new ArgumentNullException(nameof(building));
        }

        /**
         * @brief Creates a passenger and submits an elevator request on their behalf.
         *
         * @param originFloor      The floor where the passenger starts.
         * @param destinationFloor The passenger’s desired destination floor.
         * @return The newly created @ref Person instance.
         *
         * This simple implementation immediately requests an elevator once the
         * passenger is created.  In future versions, arrival time and dwell time
         * could be simulated (e.g. by scheduling the request after a delay).
         */
        public Person CreateAndRequestPassenger(int originFloor, int destinationFloor)
        {
            var person = new Person(originFloor, destinationFloor);
            // Immediately request the elevator; arrival patterns could be added later.
            building.RequestElevator(originFloor, destinationFloor);
            return person;
        }
    }
}
