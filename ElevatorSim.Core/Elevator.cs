using ElevatorSim.Common;
using System.Collections.Generic;

/*
 * How the Elevator class works
 * The Elevator class models a single elevator car’s behavior and state in the simulation. Each instance has a unique Id and tracks its current CurrentFloor, a nullable TargetFloor, and the Direction of travel (Up, Down, or Idle). Capacity sets the maximum number of passengers that can ride at once; Passengers holds the current occupants.
 * The constructor initializes the elevator’s ID, starting floor, capacity and sets it to an idle state with no destination. To assign a destination, a controller calls SetTargetFloor(floor), which records the target and sets the direction based on whether the target is above, below or equal to the current floor. The elevator doesn’t move immediately; instead, the controller periodically calls UpdatePosition(). In each call, UpdatePosition adjusts CurrentFloor by one in the direction of travel and checks whether the elevator has arrived. When it reaches the target floor, TargetFloor is cleared and the direction is reset to Idle, making the elevator free for new assignments.
 * Passenger handling is straightforward: LoadPassenger checks whether there is room (i.e. Passengers.Count < Capacity) and, if so, adds the Person to the list and notifies the passenger via Person.EnterElevator. UnloadPassenger removes a passenger from the list and calls Person.ExitElevator. Doors are represented by empty OpenDoor and CloseDoor methods for now; these are placeholders where future logic could simulate door timings or state changes.
 * The class implements IElevator, which requires generic methods (MoveToFloor, LoadPassenger(object), UnloadPassenger(object), and interface properties such as IElevator.Direction and IElevator.Passengers). In this simplified model, these interface members either throw NotImplementedException because the concrete class uses its own strongly typed methods, or they forward to the existing properties (Passengers). The IsIdle property returns true when there is no active destination. Altogether, Elevator encapsulates per‑car logic while delegating high‑level control (dispatch decisions, calling UpdatePosition on each tick) to the building’s controller.
*/

namespace ElevatorSim.Core
{
    /**
     * @brief Represents a single elevator car in the simulation.
     *
     * The Elevator class encapsulates the state of an individual car (floor position,
     * direction, current load, capacity) and exposes methods for assigning targets,
     * updating its position, and boarding or unloading passengers.  High‑level
     * movement logic (e.g. calling UpdatePosition repeatedly per simulation step)
     * is handled by the controller rather than by the elevator itself.
     */
public class Elevator : IElevator
    {
        /** @brief Unique identifier for this elevator car. Useful for logging and distinguishing multiple cars. */
        public int Id { get; }

        /**
         * @brief The floor on which the elevator currently resides.
         *
         * This property is updated by the controller (via @ref UpdatePosition)
         * as the elevator travels through the building.
         */
        public int CurrentFloor { get; private set; }

        /**
         * @brief The destination floor the elevator has been assigned.
         *
         * A null value indicates that the elevator is idle. When set, the
         * elevator should move toward this floor on subsequent update steps.
         */
        public int? TargetFloor { get; private set; }

        /**
         * @brief The elevator’s direction of travel.
         *
         * Values are Up, Down, or Idle (no movement).  The direction helps
         * dispatch algorithms determine whether the elevator can pick up
         * additional passengers en route.
         */
        public ElevatorDirection Direction { get; private set; }

        /** @brief Maximum number of passengers the elevator can carry. */
        public int Capacity { get; }

        /**
         * @brief List of passengers currently on board.
         *
         * The count of passengers in this list must never exceed @ref Capacity.
         */
        public List<Person> Passengers { get; }

        /**
         * @brief Constructs a new elevator car.
         *
         * @param id           Unique identifier for the elevator.
         * @param initialFloor Starting floor for the elevator (defaults to 1).
         * @param capacity     Maximum number of passengers (defaults to 8).
         *
         * The constructor initializes the elevator in an idle state (no target)
         * and an empty passenger list.
         */
        public Elevator(int id, int initialFloor = 1, int capacity = 8)
        {
            Id = id;
            CurrentFloor = initialFloor;
            TargetFloor = null;
            Direction = ElevatorDirection.Idle;
            Capacity = capacity;
            Passengers = new List<Person>();
        }

        /**
         * @brief Assigns a new destination floor to the elevator.
         *
         * @param floor The floor the elevator should head toward.
         *
         * This method sets @ref TargetFloor and calculates @ref Direction
         * based on the relationship between @ref CurrentFloor and the new floor.
         * The elevator does not move immediately; movement occurs when
         * @ref UpdatePosition is called by a controller.
         */
        public void SetTargetFloor(int floor)
        {
            TargetFloor = floor;
            if (floor > CurrentFloor)
            {
                Direction = ElevatorDirection.Up;
            }
            else if (floor < CurrentFloor)
            {
                Direction = ElevatorDirection.Down;
            }
            else
            {
                Direction = ElevatorDirection.Idle;
            }
        }

        /**
         * @brief Advances the elevator one step toward its target.
         *
         * This method increments or decrements @ref CurrentFloor by one, according
         * to the current @ref Direction, whenever a target is set.  If the
         * elevator reaches the target floor, @ref TargetFloor is cleared and
         * @ref Direction is set to Idle, indicating that the elevator is available
         * for new requests.
         */
        public void UpdatePosition()
        {
            if (TargetFloor.HasValue)
            {
                if (CurrentFloor < TargetFloor.Value)
                {
                    CurrentFloor++;
                    Direction = ElevatorDirection.Up;
                }
                else if (CurrentFloor > TargetFloor.Value)
                {
                    CurrentFloor--;
                    Direction = ElevatorDirection.Down;
                }
                if (CurrentFloor == TargetFloor.Value)
                {
                    TargetFloor = null;
                    Direction = ElevatorDirection.Idle;
                }
            }
        }

        /**
         * @brief Placeholder for opening the elevator door.
         *
         * In this simplified simulation, door control is handled at a higher level.
         * You could expand this method later to model door states and timing.
         */
        public void OpenDoor() { /* No operation in current model */ }

        /**
         * @brief Placeholder for closing the elevator door.
         *
         * In this simplified simulation, door control is handled at a higher level.
         * You could expand this method later to model door states and timing.
         */
        public void CloseDoor() { /* No operation in current model */ }

        /**
         * @brief Attempts to add a passenger to the elevator.
         *
         * @param person The passenger to board.
         * @return true if boarding succeeded; false if the elevator is at capacity.
         *
         * This method checks whether the elevator’s passenger list is full.
         * If there is space, the passenger is added and their state is updated
         * via @ref Person.EnterElevator; otherwise, the call returns false.
         */
        public bool LoadPassenger(Person person)
        {
            if (Passengers.Count >= Capacity)
            {
                return false;
            }
            Passengers.Add(person);
            person.EnterElevator(this);
            return true;
        }

        /**
         * @brief Removes a passenger from the elevator.
         *
         * @param person The passenger to unload.
         *
         * If the passenger is on board, they are removed from @ref Passengers and
         * their state is updated via @ref Person.ExitElevator.
         */
        public void UnloadPassenger(Person person)
        {
            if (Passengers.Remove(person))
            {
                person.ExitElevator(this);
            }
        }

        /**
         * @brief Not implemented from the interface.
         *
         * This method exists because the @ref IElevator interface requires it,
         * but the concrete class uses @ref SetTargetFloor instead.  Calling
         * @c MoveToFloor throws a NotImplementedException.
         */
        public void MoveToFloor(int floor)
        {
            throw new NotImplementedException();
        }

        /** @brief Explicit implementation of IElevator.LoadPassenger (object version). Not implemented. */
        public void LoadPassenger(object passenger)
        {
            throw new NotImplementedException();
        }

        /** @brief Explicit implementation of IElevator.UnloadPassenger (object version). Not implemented. */
        public void UnloadPassenger(object passenger)
        {
            throw new NotImplementedException();
        }

        /** @brief Indicates whether the elevator has no active target and is idle. */
        public bool IsIdle => TargetFloor == null;

        /** @brief Explicit implementation of IElevator.Direction. Not implemented here. */
        Common.ElevatorDirection IElevator.Direction => throw new NotImplementedException();

        /**
         * @brief Explicit implementation of IElevator.Passengers for object enumeration.
         *
         * The IElevator interface exposes passengers as an IEnumerable<object> to
         * allow generic dispatch strategies; this explicit implementation
         * forwards to the strongly typed @ref Passengers list.
         */
        IEnumerable<object> IElevator.Passengers
        {
            get => Passengers;
            set
            {
                Passengers.Clear();
                if (value != null)
                {
                    foreach (var obj in value)
                    {
                        if (obj is Person person)
                        {
                            Passengers.Add(person);
                        }
                    }
                }
            }
        }
    }
}
