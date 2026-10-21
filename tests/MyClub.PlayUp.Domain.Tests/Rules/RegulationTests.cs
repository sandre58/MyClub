// -----------------------------------------------------------------------
// <copyright file="RegulationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class RegulationTests
{
    [Fact]
    public void Constructor_accepts_valid_composition()
    {
        // Arrange & Act
        var regulation = SampleRegulations.Standard();

        // Assert
        regulation.EntryRules.MinimumTeams.Should().Be(2);
        regulation.MatchRules.Duration.DurationPerPeriod.Should().Be(45);
        regulation.StandingRules.Points.WinPoints.Should().Be(3);
        regulation.DisciplinaryRules.Should().Be(DisciplinaryRules.None);
    }

    [Fact]
    public void Equality_is_structural()
    {
        // Arrange & Act
        var left = SampleRegulations.Standard();
        var right = SampleRegulations.Standard();

        // Assert
        left.Should().Be(right);
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact]
    public void Distinct_instances_with_different_parts_are_not_equal()
    {
        // Arrange
        var left = SampleRegulations.Standard();
        var right = new Regulation(
            new EntryRules(4, 16),
            left.MatchRules,
            left.StandingRules);

        // Assert
        left.Should().NotBe(right);
    }
}
