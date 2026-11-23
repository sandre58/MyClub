// -----------------------------------------------------------------------
// <copyright file="NearestRankPercentilesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Development.Tests.Diagnostics;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

public sealed class NearestRankPercentilesTests
{
    [Fact]
    public void Percentile_uses_nearest_rank_on_forty_samples()
    {
        var samples = Enumerable.Range(1, 40)
            .Select(index => TimeSpan.FromMilliseconds(index))
            .ToArray();

        NearestRankPercentiles.Percentile(samples, 50).Should().Be(TimeSpan.FromMilliseconds(20));
        NearestRankPercentiles.Percentile(samples, 95).Should().Be(TimeSpan.FromMilliseconds(38));
        NearestRankPercentiles.Max(samples).Should().Be(TimeSpan.FromMilliseconds(40));
    }

    [Fact]
    public void FractionOverThreshold_counts_strictly_greater()
    {
        var samples = new[]
        {
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(4)
        };

        NearestRankPercentiles.FractionOverThreshold(samples, TimeSpan.FromSeconds(3))
            .Should()
            .BeApproximately(1 / 3.0, 0.0001);
    }
}
