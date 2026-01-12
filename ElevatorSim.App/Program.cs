using System;
using ElevatorSim.Core;
using ElevatorSim.DataIO;
using ElevatorSim.Interfaces;
using ElevatorSim.Common;

namespace ElevatorSim.App
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 1. Load configuration (use default placeholder values for now)
            var configLoader = new ConfigLoader();
            var config = configLoader.LoadConfig("config.json"); // specify a real path if needed

            // 2. Initialize building with desired dispatch strategy (use default)
            var building = new Building(
                config.NumFloors,
                config.NumElevators,
                /* dispatchStrategy */ null);

            // 3. Set up logging to console and an optional file
            building.Logger = new DataLogger("simulation.log");

            // 4. Generate initial passengers (example: three passengers)
            building.PassengerController.CreateAndRequestPassenger(1, 10);
            building.PassengerController.CreateAndRequestPassenger(5, 2);
            building.PassengerController.CreateAndRequestPassenger(8, 20);

            // 5. Run the simulation loop (e.g., 50 steps)
            int steps = 50;
            for (int t = 0; t < steps; t++)
            {
                building.ElevatorController.UpdateElevators();

                // Optionally, generate more passengers over time
                if (t % 10 == 0)
                {
                    // Example: spawn a passenger on a random floor
                    int origin = new Random().Next(1, config.NumFloors);
                    int destination = new Random().Next(1, config.NumFloors);
                    if (origin != destination)
                    {
                        building.PassengerController.CreateAndRequestPassenger(origin, destination);
                    }
                }
            }

            // 6. Finalize: flush the logs
            building.Logger.Flush();
            Console.WriteLine("Simulation complete.");
        }
    }
}
