using System;

/*
 * Detailed explanation of the code
 * This file contains two simple enumerations that are fundamental to the elevator simulation:
 * 
 * 1. ElevatorDirection – This enumeration encodes three possible motion states for an elevator:
 *      Up indicates that the elevator is travelling upward toward a higher floor.
 *      Down means the elevator is descending to a lower floor.
 *      Idle signifies that the elevator is stationary; it may be awaiting a new assignment or has just completed one.
 *      
 *      Dispatch strategies and controllers use this state to determine whether an elevator can pick up additional passengers (for example, an elevator going up can serve riders who want to go up, but not those wishing to go down).
 * 
 * 2. PersonState – This enumeration models the state of a passenger over the corse of their journey:
 *      Waiting applies to a passenger who has generated a request and is waiting on a floor for an elevator.
 *      Riding applies once the passenger has boarded an elevator and is in transit to their requested floor.
 *      Completed indicates that the passenger has reached their destination and is no longer part of the active simulation.
 *      
 *      Tracking these states allows the simulation to handle transitions like boarding (from Waiting to Riding) and disembarking (from Riding to Completed) and ensures that events (like unloading) are triggered at the correct times.
 * 
 * Together, these enums provide concise, self‑explanatory values that simplify the logic of elevator dispatch and passenger management across the system.
 */

namespace ElevatorSim.Core
{
    /**
     * @brief Represents the direction in which an elevator can travel.
     *
     * Elevators move in discrete steps between floors.  The values of this
     * enumeration indicate whether an elevator is currently travelling up,
     * travelling down, or is idle (not moving).  An idle elevator either
     * has no target or has just reached its target and is available for a new request.
     */
    public enum ElevatorDirection
    {
        Up,   /**< Elevator is moving upward. */
        Down, /**< Elevator is moving downward. */
        Idle  /**< Elevator is not moving (no active assignment). */
    }

    /**
     * @brief Represents the lifecycle state of a passenger in the simulation.
     *
     * Passengers start in the Waiting state while requesting an elevator.
     * When they board an elevator, they transition to the Riding state.  Once
     * they exit at their destination, they reach the Completed state.  These
     * states help the simulation track passenger progress and trigger appropriate
     * actions (e.g. requesting a car, boarding, unloading).
     */
    public enum PersonState
    {
        Waiting,   /**< Passenger has not yet boarded; they are waiting for an elevator. */
        Riding,    /**< Passenger is inside an elevator, en route to their destination. */
        Completed  /**< Passenger has reached their destination floor. */
    }
}
