// -----------------------------------------------------------------------
// <copyright file="NearestRankPercentiles.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Tests.Diagnostics;

/// <summary>
/// Aggregates duration samples using the nearest-rank method (no interpolation).
/// </summary>
/// <remarks>
/// For sample size <c>n</c>, rank <c>k</c> (1-based) maps to index
/// <c>ceil(k/100 × n) - 1</c> on sorted ascending ticks. p50 uses k=50, p95 uses k=95.
/// </remarks>
internal static class NearestRankPercentiles
{
    public static readonly TimeSpan ThreeSecondThreshold = TimeSpan.FromSeconds(3);

    public static TimeSpan Percentile(IReadOnlyList<TimeSpan> samples, int percentile)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (samples.Count == 0)
        {
            throw new ArgumentException("At least one sample is required.", nameof(samples));
        }

        if (percentile is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentile), percentile, "Percentile must be between 0 and 100.");
        }

        var sorted = samples.Select(sample => sample.Ticks).Order().ToArray();
        var rankIndex = (int)Math.Ceiling(percentile / 100.0 * sorted.Length) - 1;
        rankIndex = Math.Clamp(rankIndex, 0, sorted.Length - 1);
        return TimeSpan.FromTicks(sorted[rankIndex]);
    }

    public static TimeSpan Max(IReadOnlyList<TimeSpan> samples) =>
        samples.Max();

    public static double FractionOverThreshold(IReadOnlyList<TimeSpan> samples, TimeSpan threshold)
    {
        if (samples.Count == 0)
        {
            return 0;
        }

        var over = samples.Count(sample => sample > threshold);
        return over / (double)samples.Count;
    }
}
