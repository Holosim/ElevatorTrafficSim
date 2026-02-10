using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowCore.Contracts.Events;
using FlowCore.Domain.Events;
using FlowCore.Simulation.Events;

namespace FlowCore.Simulation.Adapters;

public sealed class ContractEventAdapterAsync : IAsyncDisposable
{
    private readonly int _runId;
    private long _sequence;
    private readonly List<IDisposable> _subscriptions = new();
    private readonly Func<SimEventRecord, ValueTask> _emitAsync;

    public ContractEventAdapterAsync(
        int runId,
        IEventBus bus,
        Func<SimEventRecord, ValueTask> emitAsync)
    {
        _runId = runId;
        _emitAsync = emitAsync;

        _subscriptions.Add(bus.Subscribe<CallAssignedDomainEvent>(e => _ = HandleAsync(e)));
        _subscriptions.Add(bus.Subscribe<PersonBoardedDomainEvent>(e => _ = HandleAsync(e)));
        _subscriptions.Add(bus.Subscribe<QueueSizeChangedDomainEvent>(e => _ = HandleAsync(e)));
    }

    private async Task HandleAsync(CallAssignedDomainEvent e)
    {
        var payload = new CallAssignedPayload(e.CallId, e.VehicleId, e.EstimatedPickupT);

        await EmitAsync(
            t: e.T,
            type: SimEventType.CallAssigned,
            source: e.Source,
            message: $"Call {e.CallId} assigned to vehicle {e.VehicleId}",
            payload: payload);
    }

    private async Task HandleAsync(PersonBoardedDomainEvent e)
    {
        var payload = new PersonBoardedPayload(
            e.PersonId, e.CallId, e.VehicleId, e.Floor, e.VehicleOccupantCountAfter);

        await EmitAsync(
            t: e.T,
            type: SimEventType.PersonBoarded,
            source: e.Source,
            message: $"Person {e.PersonId} boarded vehicle {e.VehicleId} at floor {e.Floor}",
            payload: payload);
    }

    private async Task HandleAsync(QueueSizeChangedDomainEvent e)
    {
        var direction = e.Direction switch
        {
            1 => FlowCore.Contracts.Common.MotionDirection.Up,
            2 => FlowCore.Contracts.Common.MotionDirection.Down,
            _ => FlowCore.Contracts.Common.MotionDirection.Idle
        };

        var payload = new QueueSizeChangedPayload(e.Floor, direction, e.NewQueueSize);

        await EmitAsync(
            t: e.T,
            type: SimEventType.QueueSizeChanged,
            source: e.Source,
            message: $"Queue changed at floor {e.Floor} ({direction}) size={e.NewQueueSize}",
            payload: payload);
    }

    private async ValueTask EmitAsync<TPayload>(
        double t,
        SimEventType type,
        string source,
        string message,
        TPayload payload)
    {
        var evt = new SimEventRecord(
            RunId: _runId,
            Sequence: Interlocked.Increment(ref _sequence),
            T: t,
            Type: type,
            Source: source,
            Message: message,
            Payload: JsonPayload.From(payload));

        await _emitAsync(evt);
    }

    public ValueTask DisposeAsync()
    {
        foreach (var s in _subscriptions) s.Dispose();
        _subscriptions.Clear();
        return ValueTask.CompletedTask;
    }
}
