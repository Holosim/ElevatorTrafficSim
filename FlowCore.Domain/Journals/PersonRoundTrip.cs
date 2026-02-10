using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Domain.Journals;

public sealed class PersonRoundTrip
{
    public int Id { get; init; }
    public int PersonId { get; init; }
    public double TotalTimeSeconds { get; set; }
    public List<TripLegRecord> Legs { get; } = new();
}


