using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Domain.Routing;

public sealed class Route
{
    public Route(IReadOnlyList<Destination> destinations)
    {
        Destinations = destinations ?? throw new ArgumentNullException(nameof(destinations));
        if (Destinations.Count == 0)
            throw new ArgumentException("Route must contain at least one destination.", nameof(destinations));
    }

    public IReadOnlyList<Destination> Destinations { get; }
}

