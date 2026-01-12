using System;
using System.Collections.Generic;
using ElevatorSim.Interfaces;
using ElevatorSim.Common;


/* How the ElevatorController operates
 * The ElevatorController serves as the central coordinator for the elevator system. Its purpose is to manage a collection of elevators (elevators) and decide which elevator should answer each passenger’s call. To achieve this:
 * Constructor and dependencies: The controller is constructed with a list of elevators and an optional IElevatorDispatchStrategy. If no strategy is provided, a default strategy is used. The controller checks that the elevator list is not null and assigns the strategy accordingly.
 * Handling requests (HandleRequest): When a passenger calls an elevator, HandleRequest is invoked with the origin floor and destination floor. The controller asks the dispatch strategy to select a suitable elevator from its list, passing in the origin and destination floors. The strategy encapsulates the decision logic—for example, it might choose the nearest idle elevator or implement load balancing. If no elevator can currently handle the request (e.g. all are full), the controller ignores the request; a more advanced version could queue it. Otherwise, it instructs the selected elevator to travel to the origin floor by calling SetTargetFloor(originFloor). In this simple model, the controller does not immediately set the passenger’s destination; a more complete implementation would remember the request and set the elevator’s next target after pickup.
 * Updating elevators (UpdateElevators): The simulation periodically calls UpdateElevators to advance the state of each elevator. For each elevator, the method stores the previous floor, calls UpdatePosition() (which moves the elevator one floor toward its current target), and checks if the elevator is now idle after moving. A change in floor combined with an Idle direction indicates the elevator reached its assigned floor. The controller then unloads passengers whose destination is the current floor. It creates a list of such passengers, iterates over the elevator’s Passengers collection to find those whose DestinationFloor matches CurrentFloor, and calls UnloadPassenger() to remove them and update their state. In a more detailed simulation, this step could also include boarding new passengers at that floor.
 * By separating the dispatch logic into a strategy (IElevatorDispatchStrategy) and keeping the controller focused on orchestrating assignments and updates, the design allows for easy swapping of scheduling algorithms and clear separation of concerns.
 */
namespace ElevatorSim.Core
{
    /**
     * @brief Central controller for elevator assignment and movement.
     *
     * This class maintains references to all elevators in the building
     * and delegates elevator selection to a dispatch strategy.  It is
     * responsible for handling pickup requests and advancing each
     * elevator’s state during the simulation.
     */
    public class ElevatorController
    {
        /** @brief The collection of elevators managed by this controller. */
        private readonly IList<IElevator> elevators;

        /** @brief Strategy used to select an elevator for a given request. */
        private readonly IElevatorDispatchStrategy dispatchStrategy;

        /**
         * @brief Creates a new ElevatorController.
         *
         * @param elevators The list of elevators to manage.  Must not be null.
         * @param strategy  Optional dispatch strategy; if null, a default strategy is used.
         *
         * Controllers are typically instantiated by the Building at start‑up.
         */
        public ElevatorController(List<IElevator> elevators, IElevatorDispatchStrategy? strategy = null)
        {
            this.elevators = elevators ?? throw new ArgumentNullException(nameof(elevators));
            this.dispatchStrategy = strategy ?? new TestDefaultDispatchStrategy();
        }

        /**
         * @brief Handles a passenger’s request for an elevator.
         *
         * @param originFloor      Floor where the passenger is waiting.
         * @param destinationFloor Passenger’s destination floor.
         *
         * This method asks the dispatch strategy to choose an elevator.  If no
         * suitable elevator is available (e.g. all are full), the request is
         * currently ignored.  Otherwise, the chosen elevator is assigned to
         * travel to the origin floor.  In a more complete system, the
         * destination would be queued and set after the passenger boards.
         */
        public void HandleRequest(int originFloor, int destinationFloor)
        {
            // Use the strategy to choose an elevator.  Strategy may return
            // null if no elevators can service the request (e.g. all at capacity).
            IElevator? selectedElevator = dispatchStrategy.SelectElevator(elevators, originFloor, destinationFloor);
            if (selectedElevator == null)
            {
                // For now, ignore the request if no elevator is available.
                // Future implementations might queue the request.
                return;
            }

            // Assign the elevator to go to the origin floor.
            selectedElevator.SetTargetFloor(originFloor);

            // In a real system, we would also track the destination so that
            // once the passenger is picked up, the elevator will go to the
            // requested floor.  This simulation can queue the request
            // until the elevator arrives at the origin.
            // TODO: queue destination for after pickup.
        }

        /**
         * @brief Advances all elevators by one simulation step.
         *
         * For each elevator, this method calls its @ref IElevator.UpdatePosition
         * to move it toward its target.  If an elevator reaches its assigned
         * target floor (i.e. it becomes idle after moving), the controller
         * unloads any passengers whose destination matches that floor.
         *
         * This update should be called repeatedly (e.g. once per simulation tick).
         */
        public void UpdateElevators()
        {
            foreach (var elevator in elevators)
            {
                int previousFloor = elevator.CurrentFloor;
                elevator.UpdatePosition();

                // Check if elevator has reached its target floor.  We consider
                // it to have arrived when its position changes and it becomes idle.
                if (previousFloor != elevator.CurrentFloor && (elevator.Direction.Equals(ElevatorDirection.Idle)))
                {
                    // Elevator arrived at target; unload passengers destined for this floor.
                    var disembarking = new List<Person>();
                    foreach (var p in elevator.Passengers)
                    {
                        if (p is Person person && person.DestinationFloor == elevator.CurrentFloor)
                        {
                            disembarking.Add(person);
                        }
                    }
                    foreach (var p in disembarking)
                    {
                        elevator.UnloadPassenger(p);
                    }
                }
            }
        }
    }
}
