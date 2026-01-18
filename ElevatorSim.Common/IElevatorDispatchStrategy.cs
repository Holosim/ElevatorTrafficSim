using ElevatorSim.Common;
using System.Collections.Generic;

/*
 * Explanation of the code
 * This code defines an extensible framework for choosing which elevator car should respond to a 
 * passenger’s request in a simulation. The IElevatorDispatchStrategy interface declares a single
 * method, SelectElevator, which takes three arguments:
 *     * A collection of IElevator objects representing all elevators currently operating in the system.
 *     * The originFloor, where the passenger is waiting.
 *     * The destinationFloor, the passenger’s intended target.
 * The method returns either an IElevator instance to handle the request or null if no elevator is 
 * available. Dispatch strategies might consider factors such as distance to the caller, 
 * current direction of each elevator, expected passenger loads, or even historical traffic patterns; 
 * the interface allows different algorithms to be implemented interchangeably.
 * 
 * DefaultDispatchStrategy implements this interface as a simple example. It loops through the list of 
 * elevators and selects the first elevator whose TargetFloor is null, which the code interprets 
 * as being idle. If no elevator is idle, it returns null, indicating the request cannot be serviced
 * immediately. This approach is naive—it doesn’t consider which elevator might arrive soonest or 
 * whether an elevator already heading toward the origin could take the passenger en route—but it 
 * demonstrates how strategies can be implemented. More sophisticated strategies can replace this 
 * default by examining elevator positions, directions and occupancy to reduce wait times and optimize
 * overall system performance.
 */

namespace ElevatorSim.Interfaces
{
    /**
     * @brief Defines a strategy for dispatching elevator requests.
     *
     * Implementations of this interface encapsulate the algorithm used to
     * decide which elevator car should respond to a passenger request.  A
     * dispatch strategy can examine the current state of all elevators—such as
     * their positions, directions, capacities and scheduled stops—and
     * determine the most suitable car to pick up a passenger at a given origin
     * floor and deliver them to a destination.
     */
public interface IElevatorDispatchStrategy
    {
        /**
         * @brief Selects an elevator to handle a pickup request.
         *
         * @param elevators        A list of elevators currently available in the system.
         * @param originFloor      The floor where the passenger is waiting.
         * @param destinationFloor The floor the passenger wishes to reach.
         * @return                 A reference to the chosen elevator, or @c null if
         *                         no suitable elevator can service the request.
         *
         * The implementation should examine the provided list of @p elevators and choose
         * an elevator that can respond.  Examples of strategies include selecting the
         * first idle elevator, the closest elevator moving toward the origin, or the
         * elevator with the fewest scheduled stops.  Returning @c null indicates that
         * no elevator is available (for instance, if they are all at capacity).
         */
        IElevator? SelectElevator(IList<IElevator> elevators, int originFloor, int destinationFloor);
    }

    /**
     * @brief Default implementation of an elevator dispatch strategy.
     *
     * This basic strategy is provided for example purposes.  It scans the list of
     * elevators and returns the first one that appears to be idle (i.e. has no
     * current target floor).  More advanced strategies could take into account
     * distance to origin floor, direction of travel, passenger loads, or other
     * criteria to optimize wait times and throughput.
     */
    public class TestDefaultDispatchStrategy : IElevatorDispatchStrategy
    {
        /** @copydoc IElevatorDispatchStrategy::SelectElevator */
        public IElevator? SelectElevator(
            IList<IElevator> elevators, int originFloor, int destinationFloor)
        {
            // Simple implementation: return the first elevator that is idle (no target).
            foreach (var elev in elevators)
            {
                if (elev.TargetFloor == null)
                {
                    return elev;
                }
            }
            // No idle elevators found; return null to indicate no available car.
            return null;
        }
    }

    public class SimpleDispatchStrategy : IElevatorDispatchStrategy
    {
        /** @copydoc IElevatorDispatchStrategy::SelectElevator */
        public IElevator? SelectElevator(
            IList<IElevator> elevators, int originFloor, int destinationFloor)
        {
            IElevator? bestElev = null;

            bool bestIsMoving = false;
            bool bestIsToward = false;
            bool bestIsPassing = false;

            foreach (var elev in elevators)
            {
                bool isIdle = elev.IsIdle;
                bool isMoving = !isIdle;
                bool isPassing = false;
                bool isToward = false;

                // Determine if this elevator will move toward or pass the origin
                if (isMoving && elev.TargetFloor.HasValue)
                {
                    if (elev.Direction == ElevatorDirection.Up)
                    {
                        if (originFloor >= elev.CurrentFloor)
                        {
                            isToward = true;
                            if (originFloor <= elev.TargetFloor.Value)
                                isPassing = true;
                        }
                    }
                    else if (elev.Direction == ElevatorDirection.Down)
                    {
                        if (originFloor <= elev.CurrentFloor)
                        {
                            isToward = true;
                            if (originFloor >= elev.TargetFloor.Value)
                                isPassing = true;
                        }
                    }
                }

                // a. Initialize best elevator if none selected yet
                if (bestElev == null)
                {
                    bestElev = elev;
                    bestIsMoving = isMoving;
                    bestIsToward = isToward;
                    bestIsPassing = isPassing;
                    continue;
                }

                // b. Prioritise elevators that will pass the origin floor
                if (isPassing)
                {
                    if (bestIsPassing)
                    {
                        // Compare distance to origin if both are passing
                        if (Math.Abs(elev.CurrentFloor - originFloor) <
                            Math.Abs(bestElev.CurrentFloor - originFloor))
                        {
                            bestElev = elev;
                        }
                    }
                    else
                    {
                        bestElev = elev;
                        bestIsPassing = true;
                    }
                    continue;
                }

                // c. Next, prefer elevators moving toward the origin floor
                if (isToward)
                {
                    if (bestIsToward)
                    {
                        if (Math.Abs(elev.CurrentFloor - originFloor) <
                            Math.Abs(bestElev.CurrentFloor - originFloor))
                        {
                            bestElev = elev;
                        }
                    }
                    else
                    {
                        bestElev = elev;
                        bestIsToward = true;
                        bestIsMoving = true;
                    }
                    continue;
                }

                // d. Then consider any moving elevator (closest target)
                if (isMoving)
                {
                    if (bestIsMoving)
                    {
                        int thisTargetDist = elev.TargetFloor.HasValue
                            ? Math.Abs(elev.TargetFloor.Value - originFloor)
                            : int.MaxValue;
                        int bestTargetDist = bestElev.TargetFloor.HasValue
                            ? Math.Abs(bestElev.TargetFloor.Value - originFloor)
                            : int.MaxValue;

                        if (thisTargetDist < bestTargetDist)
                        {
                            bestElev = elev;
                        }
                    }
                    else
                    {
                        bestElev = elev;
                        bestIsMoving = true;
                    }
                    continue;
                }

                // e. Finally, prefer idle elevators if they are closer than the current best
                // (only reached if the elevator is idle and we didn't prefer moving elevators)
                if (!isMoving)
                {
                    if (Math.Abs(elev.CurrentFloor - originFloor) <
                        Math.Abs(bestElev.CurrentFloor - originFloor))
                    {
                        bestElev = elev;
                        bestIsMoving = false;
                        bestIsToward = false;
                        bestIsPassing = false;
                    }
                }
            }

            return bestElev;
   
        }
    }
}
