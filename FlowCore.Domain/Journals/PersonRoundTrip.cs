using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Domain.Journals;

public sealed class PersonRoundTrip
{
    public required int Id { get; init; }
    public required int PersonId { get; init; }
    public double TotalTimeSeconds { get; set; }
    public List<int> TripLegRecordIds { get; } = new();
}



