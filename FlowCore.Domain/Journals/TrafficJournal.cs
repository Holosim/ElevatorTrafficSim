using FlowCore.Domain.Journals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public sealed class TrafficJournal
{
    public List<PersonRoundTrip> RoundTrips { get; } = new();

    /* Why did we move this to TripLegRecord? */
    //public List<TripLegRecord> TripLegs { get; } = new();

    public List<WaitTimeRecord> WaitTimes { get; } = new();
}
