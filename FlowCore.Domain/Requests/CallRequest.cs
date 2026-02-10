using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/* A call request is a record of a person’s request for transportation. It is created when a person initiates a call, and updated as the call progresses. It is used for tracking the status of calls, and for matching calls to vehicles. */
using FlowCore.Domain.Common;

namespace FlowCore.Domain.Requests;

public readonly record struct CallRequest(
    int CallId,
    int PersonId,
    PassengerType PersonType,
    int OriginFloor,
    int DestinationFloor,
    int Direction,       // 0 idle, 1 up, 2 down. Keep simple for now.
    double RequestT
);


/* Why did we remove the functions from this?*/

/*
using FlowCore.Domain.Common;

namespace FlowCore.Domain.Requests;

public enum CallStatus { Pending, Assigned, PickedUp, Completed, Cancelled }

public sealed class CallRequest
{
    public required int CallId { get; init; }
    public required int PersonId { get; init; }
    public required PassengerType PersonType { get; init; }

    public required int OriginFloor { get; init; }
    public required int DestinationFloor { get; init; }

    public required double RequestedAt { get; init; }

    public int? AssignedVehicleId { get; set; }
    public double? PickedUpAt { get; set; }
    public double? DroppedOffAt { get; set; }

    public CallStatus Status { get; set; } = CallStatus.Pending;
}
*/