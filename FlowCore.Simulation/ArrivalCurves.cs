using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowCore.Domain.Common;

namespace FlowCore.Simulation;

/// <summary>
/// Rate curve. Returns arrivals per second at simulation time tSim (seconds since midnight).
/// </summary>
public interface IArrivalRateCurve
{
    double RatePerSecond(double tSimSeconds);
    double MaxRatePerSecond { get; }
}

/// <summary>
/// Simple piecewise rate curve. Define segments across the day.
/// </summary>
public sealed class PiecewiseRateCurve : IArrivalRateCurve
{
    private readonly (double start, double end, double ratePerSecond)[] _segments;

    public PiecewiseRateCurve(params (double start, double end, double ratePerSecond)[] segments)
    {
        if (segments is null || segments.Length == 0)
            throw new ArgumentException("Segments must not be empty.", nameof(segments));

        _segments = segments;

        var max = 0.0;
        foreach (var s in _segments)
            if (s.ratePerSecond > max) max = s.ratePerSecond;

        MaxRatePerSecond = max;
    }

    public double MaxRatePerSecond { get; }

    public double RatePerSecond(double tSimSeconds)
    {
        foreach (var (start, end, r) in _segments)
        {
            if (tSimSeconds >= start && tSimSeconds < end)
                return r;
        }
        return 0.0;
    }
}

/// <summary>
/// Non-homogeneous Poisson arrival time generator using thinning.
/// Deterministic given Random seed.
/// </summary>
public sealed class NonHomogeneousPoisson
{
    private readonly Random _rng;

    public NonHomogeneousPoisson(Random rng)
    {
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
    }

    /// <summary>
    /// Generate the next arrival time after tSimSeconds given a rate curve lambda(t).
    /// Returns +Infinity if no arrivals occur within the horizonSeconds.
    /// </summary>
    public double NextArrivalTime(
        double tSimSeconds,
        IArrivalRateCurve curve,
        double horizonSeconds)
    {
        if (curve is null) throw new ArgumentNullException(nameof(curve));
        if (horizonSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(horizonSeconds));

        var t = tSimSeconds;
        var tEnd = tSimSeconds + horizonSeconds;

        var lambdaMax = curve.MaxRatePerSecond;
        if (lambdaMax <= 0) return double.PositiveInfinity;

        while (true)
        {
            // Sample candidate inter-arrival from Exp(lambdaMax)
            var u = NextOpen01();
            var w = -Math.Log(u) / lambdaMax;
            t += w;

            if (t >= tEnd)
                return double.PositiveInfinity;

            var lambdaT = curve.RatePerSecond(t);
            if (lambdaT <= 0)
                continue;

            // Accept with probability lambda(t) / lambdaMax
            var d = NextOpen01();
            if (d <= (lambdaT / lambdaMax))
                return t;
        }
    }

    private double NextOpen01()
    {
        // Avoid 0.0 exactly so Log doesn’t blow up
        double u;
        do u = _rng.NextDouble();
        while (u <= 0.0);
        return u;
    }
}

/// <summary>
/// Convenience: default curves by passenger type, expressed as arrivals per minute.
/// Converted to arrivals per second internally.
/// </summary>
public static class DefaultArrivalCurves
{
    private static double PerMinute(double perMin) => perMin / 60.0;

    // Time helpers
    private static double H(int hour) => hour * 3600.0;
    private static double HM(int hour, int minute) => hour * 3600.0 + minute * 60.0;

    public static IArrivalRateCurve For(PassengerType type)
    {
        // These are deliberately conservative “starter” curves.
        // You’ll tune them when you compare outcomes against the 95% < 60s requirement.

        return type switch
        {
            PassengerType.Shopper =>
                new PiecewiseRateCurve(
                    (H(0), H(10), PerMinute(0.1)),
                    (H(10), H(12), PerMinute(2.0)),
                    (H(12), H(17), PerMinute(3.5)),
                    (H(17), H(20), PerMinute(2.0)),
                    (H(20), H(24), PerMinute(0.3))
                ),

            PassengerType.OfficeWorker =>
                new PiecewiseRateCurve(
                    (H(0), HM(7, 30), PerMinute(0.05)),
                    (HM(7, 30), HM(9, 30), PerMinute(5.0)),   // morning surge
                    (HM(9, 30), HM(16, 30), PerMinute(0.2)),
                    (HM(16, 30), HM(18, 30), PerMinute(4.0)), // evening exit surge
                    (HM(18, 30), H(24), PerMinute(0.05))
                ),

            PassengerType.Resident =>
                new PiecewiseRateCurve(
                    (H(0), H(6), PerMinute(0.3)),
                    (H(6), H(9), PerMinute(1.0)),   // some leave for day
                    (H(9), H(17), PerMinute(0.4)),
                    (H(17), H(20), PerMinute(1.2)),  // return home
                    (H(20), H(24), PerMinute(0.6))
                ),

            _ => new PiecewiseRateCurve((0, H(24), 0))
        };
    }
}

