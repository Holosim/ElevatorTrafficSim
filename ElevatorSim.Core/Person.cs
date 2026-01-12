/*
 * Explanation of the Person class
 * The Person class models a single passenger in the elevator simulation. It uses a static field nextId to assign a unique PersonId to each new instance—useful for logging or debugging when multiple passengers exist. The class stores immutable information about where the passenger starts (OriginFloor) and where they want to go (DestinationFloor).
 * A Person also tracks its current State using the PersonState enumeration (Waiting, Riding, Completed). Passengers begin in the Waiting state as soon as they are created. When the simulation logic calls EnterElevator, the passenger’s state is set to Riding, indicating they have boarded an elevator. When the passenger reaches their destination and exits, the simulation calls ExitElevator, which sets the state to Completed. These transitions allow the rest of the system to know whether a passenger needs to be picked up, is in transit, or has finished their journey. The Person class itself does not move the passenger; it simply stores data and updates state when instructed by higher-level controllers.
 */

namespace ElevatorSim.Core
{
    /**
     * @brief Represents a person in the elevator simulation.
     *
     * Each passenger has an origin floor and a destination floor, and their
     * progress through the simulation is tracked via the @ref PersonState
     * enumeration.  Passengers begin in the Waiting state, transition to
     * Riding when they enter an elevator, and end in Completed once they
     * exit at their destination.
     */
public class Person
    {
        /** @brief Static counter used to assign unique IDs to passengers. */
        private static int nextId = 1;

        /** @brief Unique identifier for this passenger (useful for logging/debugging). */
        public int PersonId { get; }

        /** @brief The floor on which the passenger starts their journey. */
        public int OriginFloor { get; }

        /** @brief The floor the passenger wants to reach. */
        public int DestinationFloor { get; }

        /**
         * @brief The passenger’s current state in the simulation.
         *
         * See @ref PersonState for possible values.
         */
        public PersonState State { get; private set; }

        /**
         * @brief Constructs a new passenger.
         *
         * @param originFloor      The floor where the passenger begins.
         * @param destinationFloor The floor the passenger wishes to go to.
         *
         * The constructor assigns a unique ID, sets the origin and destination
         * floors, and initializes the state to @c PersonState.Waiting.
         */
        public Person(int originFloor, int destinationFloor)
        {
            PersonId = nextId++;
            OriginFloor = originFloor;
            DestinationFloor = destinationFloor;
            State = PersonState.Waiting;
        }

        /**
         * @brief Called when the passenger boards an elevator.
         *
         * @param elevator The elevator the passenger is boarding.
         *
         * This method updates the passenger’s state to @c PersonState.Riding.
         * It does not interact with the elevator directly; the elevator’s own
         * methods handle adding the passenger to its list.
         */
        public void EnterElevator(Elevator elevator)
        {
            State = PersonState.Riding;
        }

        /**
         * @brief Called when the passenger exits an elevator at their destination.
         *
         * @param elevator The elevator the passenger is leaving.
         *
         * This method updates the passenger’s state to @c PersonState.Completed.
         */
        public void ExitElevator(Elevator elevator)
        {
            State = PersonState.Completed;
        }
    }
}
