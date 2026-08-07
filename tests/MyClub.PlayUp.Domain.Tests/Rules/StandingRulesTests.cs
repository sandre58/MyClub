// -----------------------------------------------------------------------
// <copyright file="StandingRulesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class StandingRulesTests
{
    private static PointsPolicy StandardPoints() => new(3, 1, 0);

    [Fact]
    public void Ranking_criteria_preserve_order()
    {
        // Arrange
        var criteria = new[]
        {
            RankingCriterion.HeadToHead,
            RankingCriterion.Points,
            RankingCriterion.GoalDifference
        };

        // Act
        var rules = new StandingRules(StandardPoints(), criteria);

        // Assert
        rules.RankingCriteria.Should().Equal(criteria);
        rules.Points.WinPoints.Should().Be(3);
    }

    [Fact]
    public void Empty_ranking_criteria_are_rejected()
    {
        // Arrange & Act
        var act = () => new StandingRules(StandardPoints(), []);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.RankingCriteriaInvalid);
    }

    [Fact]
    public void Duplicate_ranking_criteria_are_rejected()
    {
        // Arrange & Act
        var act = () => new StandingRules(
            StandardPoints(),
            [RankingCriterion.Points, RankingCriterion.Points]);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.RankingCriteriaInvalid);
    }

    [Fact]
    public void Undefined_ranking_criterion_is_rejected()
    {
        // Arrange & Act
        var act = () => new StandingRules(
            StandardPoints(),
            [(RankingCriterion)999]);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.RankingCriteriaInvalid);
    }

    [Fact]
    public void Equality_is_structural_including_criteria_order()
    {
        // Arrange
        var points = StandardPoints();
        var left = new StandingRules(points, [RankingCriterion.Points, RankingCriterion.GoalsFor]);
        var same = new StandingRules(new PointsPolicy(3, 1, 0), [RankingCriterion.Points, RankingCriterion.GoalsFor]);
        var differentOrder = new StandingRules(points, [RankingCriterion.GoalsFor, RankingCriterion.Points]);

        // Assert
        left.Should().Be(same);
        left.Should().NotBe(differentOrder);
    }
}
