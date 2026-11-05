// -----------------------------------------------------------------------
// <copyright file="PerformanceBaselineStatistics.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Tests.Diagnostics;

/// <summary>
/// Deterministic aggregation for performance baseline samples (high-resolution durations).
/// </summary>
internal static class PerformanceBaselineStatistics
{
    public static TimeSpan Median(IReadOnlyList<TimeSpan> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (samples.Count == 0)
        {
            throw new ArgumentException("At least one sample is required.", nameof(samples));
        }

        if (samples.Count % 2 == 0)
        {
            throw new ArgumentException("Median requires an odd number of samples.", nameof(samples));
        }

        var sorted = samples.Select(s => s.Ticks).Order().ToArray();
        return TimeSpan.FromTicks(sorted[sorted.Length / 2]);
    }

    public static bool IsStrictlyUnderBudget(TimeSpan median, TimeSpan budget) => median < budget;
}
