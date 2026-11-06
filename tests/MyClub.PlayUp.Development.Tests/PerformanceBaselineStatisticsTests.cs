// -----------------------------------------------------------------------
// <copyright file="PerformanceBaselineStatisticsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Development.Tests.Diagnostics;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

public sealed class PerformanceBaselineStatisticsTests
{
    [Fact]
    public void Median_requires_odd_sample_count()
    {
        var act = () => PerformanceBaselineStatistics.Median(
        [
            TimeSpan.FromMilliseconds(1),
            TimeSpan.FromMilliseconds(2)
        ]);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("samples");
    }

    [Fact]
    public void Median_uses_middle_value_on_unsorted_nine_samples()
    {
        var samples = new[]
        {
            TimeSpan.FromMilliseconds(120),
            TimeSpan.FromMilliseconds(80),
            TimeSpan.FromMilliseconds(95),
            TimeSpan.FromMilliseconds(110),
            TimeSpan.FromMilliseconds(90),
            TimeSpan.FromMilliseconds(105),
            TimeSpan.FromMilliseconds(85),
            TimeSpan.FromMilliseconds(115),
            TimeSpan.FromMilliseconds(100)
        };

        PerformanceBaselineStatistics.Median(samples).Should().Be(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void Median_is_robust_to_one_isolated_spike()
    {
        var samples = new[]
        {
            TimeSpan.FromMilliseconds(90),
            TimeSpan.FromMilliseconds(92),
            TimeSpan.FromMilliseconds(91),
            TimeSpan.FromMilliseconds(93),
            TimeSpan.FromMilliseconds(94),
            TimeSpan.FromMilliseconds(95),
            TimeSpan.FromMilliseconds(96),
            TimeSpan.FromMilliseconds(97),
            TimeSpan.FromMilliseconds(400)
        };

        PerformanceBaselineStatistics.Median(samples).Should().Be(TimeSpan.FromMilliseconds(94));
    }

    [Fact]
    public void IsStrictlyUnderBudget_rejects_median_at_budget_boundary()
    {
        var median = TimeSpan.FromMilliseconds(150);
        var budget = TimeSpan.FromMilliseconds(150);

        PerformanceBaselineStatistics.IsStrictlyUnderBudget(median, budget).Should().BeFalse();
    }

    [Fact]
    public void IsStrictlyUnderBudget_accepts_median_just_under_budget_with_sub_millisecond_precision()
    {
        var median = TimeSpan.FromTicks(TimeSpan.FromMilliseconds(150).Ticks - 1);
        var budget = TimeSpan.FromMilliseconds(150);

        PerformanceBaselineStatistics.IsStrictlyUnderBudget(median, budget).Should().BeTrue();
        median.TotalMilliseconds.Should().BeApproximately(149.9999, 0.0001);
    }

    [Fact]
    public void IsStrictlyUnderBudget_rejects_sustained_slow_series_median()
    {
        var samples = Enumerable.Range(0, 9)
            .Select(_ => TimeSpan.FromMilliseconds(160))
            .ToArray();

        var median = PerformanceBaselineStatistics.Median(samples);
        PerformanceBaselineStatistics.IsStrictlyUnderBudget(median, TimeSpan.FromMilliseconds(150))
            .Should()
            .BeFalse();
    }
}
