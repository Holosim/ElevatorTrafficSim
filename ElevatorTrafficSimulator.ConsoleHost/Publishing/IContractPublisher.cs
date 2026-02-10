using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowCore.Contracts.Events;
using FlowCore.Contracts.Snapshots;

namespace ElevatorTrafficSimulator.ConsoleHost.Publishing;

public interface IContractPublisher : IAsyncDisposable
{
    ValueTask PublishEventAsync(SimEventRecord evt, CancellationToken ct = default);
    ValueTask PublishSnapshotAsync(SimTickSnapshot snap, CancellationToken ct = default);
}

