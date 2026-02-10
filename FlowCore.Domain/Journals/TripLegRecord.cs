using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowCore.Domain.Common;

namespace FlowCore.Domain.Journals;

public readonly record struct TripLegRecord(
    int Id,
    int PersonId,
    int FromFloor,
    int ToFloor,
    double WaitSeconds,
    double RideSeconds
);

/* Why did we remove the functions from this?*/

//namespace FlowCore.Domain.Journals;

//public sealed class TripLegRecord
//{
//    public required int Id { get; init; }
//    public required int PersonId { get; init; }
//    public required PassengerType PersonType { get; init; }

//    public required int OriginFloor { get; init; }
//    public required int DestinationFloor { get; init; }

//    public required double RequestedAt { get; init; }
//    public required double PickedUpAt { get; init; }
//    public required double DroppedOffAt { get; init; }

//    public double WaitSeconds => PickedUpAt - RequestedAt;
//    public double RideSeconds => DroppedOffAt - PickedUpAt;
//}

