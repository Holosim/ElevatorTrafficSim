namespace ElevatorSim.Common
{
    /**
     * @brief Possible movement states for an elevator.
     *
     * This enumeration defines the direction in which an elevator can travel:
     * Up, Down, or Idle (not moving).  It allows code using the @ref IElevator
     * interface to determine and control motion.
     */
    public enum ElevatorDirection { Up, Down, Idle }

    /**
     * @brief Contract for elevator implementations.
     *
     * The @c IElevator interface defines the core properties and operations that any
     * elevator class must implement in the simulation.  It abstracts away details
     * of a concrete elevator so that controllers and dispatch strategies can work
     * with any elevator implementation through this common API.
     */
    public interface IElevator
    {
        /**
         * @brief Unique identifier for this elevator.
         *
         * Each elevator in the building is assigned an integer ID so it can be
         * distinguished from others.  Implementations should ensure that IDs are
         * unique within a system.
         */
        int Id { get; }

        /**
         * @brief Gets the current floor on which the elevator is located.
         *
         * This property reflects the elevator’s present position in the building.
         * It may change over time as @ref MoveToFloor or @ref UpdatePosition is called.
         */
        int CurrentFloor { get; }

        /**
         * @brief Indicates the current direction of travel.
         *
         * Returns @ref ElevatorDirection.Up, @ref ElevatorDirection.Down or
         * @ref ElevatorDirection.Idle to represent the elevator’s motion status.
         * When no target is assigned, the direction should be Idle.
         */
        ElevatorDirection Direction { get; }

        /**
         * @brief Maximum number of passengers the elevator can carry.
         *
         * Implementations must enforce capacity constraints when loading passengers.
         */
        int Capacity { get; }

        /**
         * @brief Floor to which the elevator is currently headed.
         *
         * This nullable property holds the destination floor set via
         * @ref SetTargetFloor.  When the elevator has no active request, it should be null.
         */
        int? TargetFloor { get; }

        /**
         * @brief Indicates whether the elevator is idle.
         *
         * Returns true when the elevator has no target and is not moving.
         * It is a convenience property that may be implemented as:
         * @code return TargetFloor == null && Direction == ElevatorDirection.Idle; @endcode
         */
        bool IsIdle { get; }

        /**
         * @brief Collection of passengers currently in the elevator.
         *
         * This property exposes a read/write collection of passenger objects.
         * Each object can represent a passenger entity in the simulation.
         * Implementations should ensure that the number of passengers does not
         * exceed @ref Capacity and that passengers are updated when they board or leave.
         */
        IEnumerable<object> Passengers { get; set; }

        /**
         * @brief Assigns a new destination floor.
         *
         * @param floor The floor the elevator should travel to next.
         *
         * This method sets @ref TargetFloor and typically triggers the elevator to
         * begin moving toward that floor if it is currently idle or between trips.
         */
        void SetTargetFloor(int floor);

        /**
         * @brief Moves the elevator immediately to the specified floor.
         *
         * @param floor The floor number to move to.
         *
         * This method is intended to represent the elevator moving (or being
         * teleported) to a specific floor.  It might update @ref CurrentFloor directly
         * or could trigger a sequence of movements if yor simulation tracks
         * intermediate floors.  It can be used by a controller when the elevator
         * reaches its target or needs to reposition.
         */
        void MoveToFloor(int floor);

        /**
         * @brief Loads a passenger into the elevator.
         *
         * @param passenger An object representing the passenger to be loaded.
         *
         * Implementations should add the passenger to @ref Passengers if there is
         * available capacity and update any internal state needed for tracking.
         */
        void LoadPassenger(object passenger);

        /**
         * @brief Unloads a passenger from the elevator.
         *
         * @param passenger The passenger object to remove.
         *
         * Implementations should remove the passenger from @ref Passengers and
         * update the elevator’s state accordingly.  Typically called when the
         * elevator reaches the passenger’s destination floor.
         */
        void UnloadPassenger(object passenger);

        /**
         * @brief Advances the elevator’s position one simulation step.
         *
         * This method should update @ref CurrentFloor and @ref Direction based on the
         * elevator’s movement toward @ref TargetFloor.  It can also handle logic for
         * opening doors, stopping at intermediate floors, or becoming idle when
         * the destination has been reached.  Controllers call this method during
         * each time step to animate the elevator through its motion.
         */
        void UpdatePosition();
    }
}
