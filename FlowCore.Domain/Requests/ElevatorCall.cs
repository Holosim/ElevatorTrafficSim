using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowCore.Domain.Common;

namespace FlowCore.Domain.Requests;

public enum CallStatus { Pending, Assigned, PickedUp, Completed, Cancelled }

public sealed class ElevatorCall
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
