// -----------------------------------------------------------------------
// <copyright file="StandingRuleSetTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matchs;
using MyClub.Shared.Domain.Standings.Comparers;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;
using MyClub.Tests.Common;
using Xunit;

namespace MyClub.Shared.Tests.Standings;

public class StandingRuleSetTests : TestBase
{
    [Fact]
    public void DefaultPoints_ShouldContainStandardValues()
    {
        var points = StandingRuleSet.DefaultPoints;
        points.Should().ContainKey(MatchResultType.Win).WhoseValue.Should().Be(3);
        points.Should().ContainKey(MatchResultType.Draw).WhoseValue.Should().Be(1);
        points.Should().ContainKey(MatchResultType.Loss).WhoseValue.Should().Be(0);
        points.Should().ContainKey(MatchResultType.Withdraw).WhoseValue.Should().Be(-1);
    }

    [Fact]
    public void Default_ShouldHaveExpectedProperties()
    {
        var ruleSet = StandingRuleSet.Default;
        ruleSet.PointsByOutcome.Should().BeEquivalentTo(StandingRuleSet.DefaultPoints);
        ruleSet.Columns.Should().NotBeNullOrEmpty();
        ruleSet.Comparer.Should().NotBeNull();
    }

    [Fact]
    public void GetPoints_ShouldReturnCorrectPoints()
    {
        var ruleSet = StandingRuleSet.Default;
        ruleSet.GetPoints(MatchResultType.Win).Should().Be(3);
        ruleSet.GetPoints(MatchResultType.Draw).Should().Be(1);
        ruleSet.GetPoints(MatchResultType.Loss).Should().Be(0);
        ruleSet.GetPoints(MatchResultType.Withdraw).Should().Be(-1);
    }

    [Fact]
    public void GetColumn_ShouldReturnCorrectColumn()
    {
        var ruleSet = StandingRuleSet.Default;
        var column = ruleSet.GetColumn("GamesWon");
        column.Should().NotBeNull();
        column!.Key.Should().Be("GamesWon");
    }

    [Fact]
    public void GetColumn_ShouldReturnNullForUnknownColumn()
    {
        var ruleSet = StandingRuleSet.Default;
        var column = ruleSet.GetColumn("UnknownColumn");
        column.Should().BeNull();
    }

    [Fact]
    public void ComputePoints_ShouldSumPointsForTeam()
    {
        var teamId = new TeamId(Guid.NewGuid());
        var matches = new List<IMatch>();

        var mockMatch1 = new Mock<IMatch>();
        mockMatch1.Setup(m => m.GetResultOf(teamId)).Returns(MatchResultType.Win);
        var mockMatch2 = new Mock<IMatch>();
        mockMatch2.Setup(m => m.GetResultOf(teamId)).Returns(MatchResultType.Draw);
        var mockMatch3 = new Mock<IMatch>();
        mockMatch3.Setup(m => m.GetResultOf(teamId)).Returns(MatchResultType.Loss);

        matches.Add(mockMatch1.Object);
        matches.Add(mockMatch2.Object);
        matches.Add(mockMatch3.Object);

        var ruleSet = StandingRuleSet.Default;
        var totalPoints = ruleSet.ComputePoints(teamId, matches);

        totalPoints.Should().Be(3 + 1 + 0);
    }

    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        var pointsByOutcome = new Dictionary<MatchResultType, int>
        {
            { MatchResultType.Win, 5 },
            { MatchResultType.Draw, 2 },
            { MatchResultType.Loss, 0 }
        };
        var columns = StandingRuleSet.DefaultColumns;
        var comparer = StandingComparer.Default;

        var ruleSet = new StandingRuleSet(pointsByOutcome, columns, comparer);

        ruleSet.PointsByOutcome.Should().BeEquivalentTo(pointsByOutcome);
        ruleSet.Columns.Should().BeEquivalentTo(columns);
        ruleSet.Comparer.Should().BeEquivalentTo(comparer);
    }
}
