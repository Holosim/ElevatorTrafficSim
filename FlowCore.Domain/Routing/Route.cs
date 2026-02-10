using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Domain.Routing;

/* A route owns an ordered list of destinations. It is immutable in structure, but maintains a cursor (index). This is the “reuse routes” foundation later. */
public sealed class Route
{
    private readonly IReadOnlyList<Destination> _destinations;

    public Route(IReadOnlyList<Destination> destinations)
    {
        if (destinations is null) throw new ArgumentNullException(nameof(destinations));
        if (destinations.Count == 0) throw new ArgumentException("Route must contain at least one destination.", nameof(destinations));

        // Make a defensive copy so callers cannot mutate our route after construction.
        _destinations = new ReadOnlyCollection<Destination>(destinations.ToArray());
    }

    public IReadOnlyList<Destination> Destinations => _destinations;

    public int Count => _destinations.Count;
}
