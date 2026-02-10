using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowCore.Contracts.Common;

namespace FlowCore.Contracts.Reports;

public readonly record struct SimulationReport(
    int RunId,
    double SimDurationSeconds,

    double AvgWaitSecondsOverall,
    double WaitSecondsP95,
    double PercentWaitUnder60,

    IReadOnlyDictionary<PassengerType, double> AvgWaitSecondsByType,

    IReadOnlyDictionary<int, int> MaxQueueByFloorUp,
    IReadOnlyDictionary<int, int> MaxQueueByFloorDown,

    IReadOnlyDictionary<int, int> CapacityHitCountByVehicle,
    IReadOnlyDictionary<int, double> UtilizationByVehicle
);
