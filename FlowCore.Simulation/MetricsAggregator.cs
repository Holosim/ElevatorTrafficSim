using FlowCore.Domain.Common;
using FlowCore.Domain.Events;
using FlowCore.Domain.Requests;
using FlowCore.Simulation.Events;

namespace FlowCore.Simulation;

public sealed class MetricsAggregator : IDisposable
{
    private readonly double _waitTargetSeconds;

    private readonly Dictionary<int, CallMeta> _calls = new();        // CallId -> (request time, type)
    private readonly Dictionary<int, double> _boardedAt = new();      // CallId -> boarded time
    private readonly List<double> _waitSamples = new();
    private readonly List<double> _rideSamples = new();

    private readonly Dictionary<PassengerType, List<double>> _waitByType = new();

    private readonly List<IDisposable> _subs = new();

    private sealed record CallMeta(double RequestT, PassengerType Type);

    public MetricsAggregator(double waitTargetSeconds = 60.0)
    {
        _waitTargetSeconds = waitTargetSeconds;
    }

    /// <summary>
    /// Register call submissions so we can compute wait time later when boarded arrives.
    /// </summary>
    public void RecordCallSubmitted(CallRequest call)
    {
        _calls[call.CallId] = new CallMeta(call.RequestT, call.PersonType);
    }

    public void Subscribe(IEventBus bus)
    {
        if (bus is null) throw new ArgumentNullException(nameof(bus));

        _subs.Add(bus.Subscribe<PersonBoardedDomainEvent>(OnBoarded));
        _subs.Add(bus.Subscribe<PersonAlightedDomainEvent>(OnAlighted));
    }

    private void OnBoarded(PersonBoardedDomainEvent e)
    {
        _boardedAt[e.CallId] = e.T;

        if (_calls.TryGetValue(e.CallId, out var meta))
        {
            var wait = e.T - meta.RequestT;
            if (wait >= 0)
            {
                _waitSamples.Add(wait);

                if (!_waitByType.TryGetValue(meta.Type, out var list))
                {
                    list = new List<double>();
                    _waitByType[meta.Type] = list;
                }
                list.Add(wait);
            }
        }
    }

    private void OnAlighted(PersonAlightedDomainEvent e)
    {
        if (_boardedAt.TryGetValue(e.CallId, out var boardedT))
        {
            var ride = e.T - boardedT;
            if (ride >= 0)
                _rideSamples.Add(ride);
        }
    }

    public SimulationWaitReport BuildWaitReport()
    {
        var overall = ComputeStats(_waitSamples, _waitTargetSeconds);

        var byType = new Dictionary<PassengerType, WaitStats>();
        foreach (var kvp in _waitByType)
            byType[kvp.Key] = ComputeStats(kvp.Value, _waitTargetSeconds);

        var ride = ComputeStats(_rideSamples, targetSeconds: double.NaN);

        return new SimulationWaitReport(
            WaitTargetSeconds: _waitTargetSeconds,
            OverallWait: overall,
            WaitByType: byType,
            OverallRide: ride);
    }

    private static WaitStats ComputeStats(List<double> samples, double targetSeconds)
    {
        if (samples.Count == 0)
            return new WaitStats(Count: 0, Avg: double.NaN, P95: double.NaN, UnderTargetPct: double.NaN);

        double sum = 0;
        int under = 0;

        for (int i = 0; i < samples.Count; i++)
        {
            var v = samples[i];
            sum += v;
            if (!double.IsNaN(targetSeconds) && v <= targetSeconds)
                under++;
        }

        var avg = sum / samples.Count;
        var p95 = Percentile(samples, 0.95);

        var underPct = double.IsNaN(targetSeconds)
            ? double.NaN
            : (100.0 * under / samples.Count);

        return new WaitStats(samples.Count, avg, p95, underPct);
    }

    private static double Percentile(List<double> samples, double p)
    {
        // Nearest-rank on sorted copy.
        var arr = samples.ToArray();
        Array.Sort(arr);

        var n = arr.Length;
        if (n == 0) return double.NaN;

        var rank = (int)Math.Ceiling(p * n); // 1..n
        rank = Math.Clamp(rank, 1, n);

        return arr[rank - 1];
    }

    public void Dispose()
    {
        foreach (var s in _subs)
            s.Dispose();
        _subs.Clear();
    }
}

public sealed record SimulationWaitReport(
    double WaitTargetSeconds,
    WaitStats OverallWait,
    IReadOnlyDictionary<PassengerType, WaitStats> WaitByType,
    WaitStats OverallRide
);

public sealed record WaitStats(
    int Count,
    double Avg,
    double P95,
    double UnderTargetPct
);
