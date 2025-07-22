// -----------------------------------------------------------------------
// <copyright file="StandingCalculatorTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matchs;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Standings.Services;
using MyClub.Shared.Domain.Teams;
using Xunit;

namespace MyClub.Shared.Tests.Standings;

public class StandingCalculatorTests
{
    [Fact]
    public void Calculate_ShouldReturnStandingWithCorrectTeams()
    {
        // Arrange
        var team1 = new TeamId(Guid.NewGuid()).ToReference();
        var team2 = new TeamId(Guid.NewGuid()).ToReference();
        var teams = new[] { team1, team2 };
        var matches = new List<IMatch>();
        var rules = StandingRuleSet.Default;

        var calculator = new StandingCalculator();

        // Act
        var standing = calculator.Calculate(teams, matches, rules);

        // Assert
        standing.Should().NotBeNull();
        standing.Count.Should().Be(2);
        standing.Should().Contain(row => row.Team == team1);
        standing.Should().Contain(row => row.Team == team2);
    }

    [Fact]
    public void Calculate_ShouldApplyMatchesAndComputePoints()
    {
        // Arrange
        var team1 = new TeamId(Guid.NewGuid()).ToReference();
        var team2 = new TeamId(Guid.NewGuid()).ToReference();
        var teams = new[] { team1, team2 };

        var mockMatch = new Mock<IMatch>();
        mockMatch.Setup(m => m.HasResult()).Returns(true);
        mockMatch.Setup(m => m.GetTeams()).Returns([team1, team2]);
        mockMatch.Setup(m => m.GetResultOf(team1)).Returns(MatchResultType.Win);
        mockMatch.Setup(m => m.GetResultOf(team2)).Returns(MatchResultType.Loss);

        var matches = new List<IMatch> { mockMatch.Object };
        var rules = StandingRuleSet.Default;

        var calculator = new StandingCalculator();

        // Act
        var standing = calculator.Calculate(teams, matches, rules);

        // Assert
        standing.GetRow(team1).Points.Should().BeGreaterThan(standing.GetRow(team2).Points);
    }

    [Fact]
    public void Calculate_ShouldApplyPenaltyPoints()
    {
        // Arrange
        var team1 = new TeamId(Guid.NewGuid()).ToReference();
        var team2 = new TeamId(Guid.NewGuid()).ToReference();
        var teams = new[] { team1, team2 };

        var mockMatch = new Mock<IMatch>();
        mockMatch.Setup(m => m.HasResult()).Returns(true);
        mockMatch.Setup(m => m.GetTeams()).Returns([team1, team2]);
        mockMatch.Setup(m => m.GetResultOf(It.IsAny<TeamReference>())).Returns(MatchResultType.Draw);

        var matches = new List<IMatch> { mockMatch.Object };
        var rules = StandingRuleSet.Default;
        var penaltyPoints = new Dictionary<TeamReference, int> { { team1, 2 } };

        var calculator = new StandingCalculator();

        // Act
        var standing = calculator.Calculate(teams, matches, rules, penaltyPoints);

        // Assert
        standing.GetRow(team1).PenaltyPoints.Should().Be(2);
        standing.GetRow(team1).Points.Should().Be(rules.GetPoints(MatchResultType.Draw) - 2);
    }

    [Fact]
    public void Calculate_ShouldWorkWithEmptyMatches()
    {
        // Arrange
        var team1 = new TeamId(Guid.NewGuid()).ToReference();
        var teams = new[] { team1 };
        var matches = new List<IMatch>();
        var rules = StandingRuleSet.Default;

        var calculator = new StandingCalculator();

        // Act
        var standing = calculator.Calculate(teams, matches, rules);

        // Assert
        standing.Count.Should().Be(1);
        standing.GetRow(team1).Points.Should().Be(0);
    }
}
