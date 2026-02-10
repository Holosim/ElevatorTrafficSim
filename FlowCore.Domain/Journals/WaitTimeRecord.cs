using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Domain.Journals;

public sealed class WaitTimeRecord
{
    public required int Id { get; init; }
    public required int PersonId { get; init; }
    public required int FloorId { get; init; }
    public required int TargetDestination { get; init; }

    public required double RequestedAt { get; init; }
    public required double PickedUpAt { get; init; }

    public required double TimeInQueueSeconds { get; init; }
    public required int VehicleId { get; init; }
}

