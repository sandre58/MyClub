// -----------------------------------------------------------------------
// <copyright file="StandingTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Domain.Standings;
using MyClub.Shared.Domain.Teams;
using Xunit;

namespace MyClub.Shared.Tests.Domain.Standings;

public class StandingTests
{
    [Fact]
    public void Constructor_ShouldInitializeRowsForEachTeam()
    {
        var teamIds = new[] { TeamId.New().ToReference(), TeamId.New().ToReference() };
        var standing = new Standing(teamIds);

        standing.Count.Should().Be(2);
        standing.Should().OnlyContain(row => teamIds.Contains(row.Team));
    }

    [Fact]
    public void GetRow_ShouldReturnCorrectStandingRow()
    {
        var teamId = TeamId.New().ToReference();
        var standing = new Standing([teamId]);

        var row = standing.GetRow(teamId);

        row.Should().NotBeNull();
        row.Team.Should().Be(teamId);
    }

    [Fact]
    public void Contains_ShouldReturnTrueIfTeamExists()
    {
        var teamId = TeamId.New().ToReference();
        var standing = new Standing([teamId]);

        standing.Contains(teamId).Should().BeTrue();
        standing.Contains(TeamId.New().ToReference()).Should().BeFalse();
    }

    [Fact]
    public void GetColumn_ShouldReturnValueFromRow()
    {
        var teamId = TeamId.New().ToReference();
        var standing = new Standing([teamId]);
        var row = standing.GetRow(teamId);
        const string column = "Points";

        // Set value via reflection for test
        typeof(StandingRow).GetField("_columns", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(row, new Dictionary<string, object> { { column, 42 } });

        standing.GetColumn<int>(teamId, column).Should().Be(42);
    }

    [Fact]
    public void GetColumn_StandingColumnType_ShouldReturnValueFromRow()
    {
        var teamId = TeamId.New().ToReference();
        var standing = new Standing([teamId]);
        var row = standing.GetRow(teamId);
        const string column = nameof(StandingColumnType.GoalsFor);

        typeof(StandingRow).GetField("_columns", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(row, new Dictionary<string, object> { { column, 7 } });

        standing.GetColumn(teamId, StandingColumnType.GoalsFor).Should().Be(7);
    }

    [Fact]
    public void ApplyMatch_ShouldUpdateRowsAndSetContext()
    {
        var teamId1 = TeamId.New().ToReference();
        var teamId2 = TeamId.New().ToReference();
        var standing = new Standing([teamId1, teamId2]);

        var mockMatch = new Mock<IMatch>();
        mockMatch.Setup(static m => m.HasResult()).Returns(true);
        mockMatch.Setup(static m => m.GetTeams()).Returns([teamId1, teamId2]);
        mockMatch.Setup(static m => m.GetResultOf(It.IsAny<TeamReference>())).Returns(MatchResultType.Win);

        standing.ApplyMatch(mockMatch.Object);

        standing.GetRow(teamId1).Points.Should().NotBe(0);
        standing.GetRow(teamId2).Points.Should().NotBe(0);
    }

    [Fact]
    public void ApplyMatches_ShouldApplyAllMatches()
    {
        var teamId1 = TeamId.New().ToReference();
        var teamId2 = TeamId.New().ToReference();
        var standing = new Standing([teamId1, teamId2]);

        var mockMatch1 = new Mock<IMatch>();
        mockMatch1.Setup(static m => m.HasResult()).Returns(true);
        mockMatch1.Setup(static m => m.GetTeams()).Returns([teamId1, teamId2]);
        mockMatch1.Setup(static m => m.GetResultOf(It.IsAny<TeamReference>())).Returns(MatchResultType.Win);

        var mockMatch2 = new Mock<IMatch>();
        mockMatch2.Setup(static m => m.HasResult()).Returns(true);
        mockMatch2.Setup(static m => m.GetTeams()).Returns([teamId1, teamId2]);
        mockMatch2.Setup(static m => m.GetResultOf(It.IsAny<TeamReference>())).Returns(MatchResultType.Draw);

        standing.ApplyMatches([mockMatch1.Object, mockMatch2.Object]);

        standing.GetRow(teamId1).Points.Should().BeGreaterThan(0);
        standing.GetRow(teamId2).Points.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ComputeAll_ShouldComputeRowsForAllTeams()
    {
        var teamId1 = TeamId.New().ToReference();
        var teamId2 = TeamId.New().ToReference();
        var standing = new Standing([teamId1, teamId2]);

        var mockMatch = new Mock<IMatch>();
        mockMatch.Setup(static m => m.HasResult()).Returns(true);
        mockMatch.Setup(static m => m.GetTeams()).Returns([teamId1, teamId2]);
        mockMatch.Setup(static m => m.GetResultOf(It.IsAny<TeamReference>())).Returns(MatchResultType.Win);

        standing.ComputeAll([mockMatch.Object]);

        standing.GetRow(teamId1).Points.Should().BeGreaterThan(0);
        standing.GetRow(teamId2).Points.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetRank_ShouldReturnCorrectRank()
    {
        var teamId1 = TeamId.New().ToReference();
        var teamId2 = TeamId.New().ToReference();
        var standing = new Standing([teamId1, teamId2]);

        var mockMatch = new Mock<IMatch>();
        mockMatch.Setup(m => m.HasResult()).Returns(true);
        mockMatch.Setup(m => m.GetTeams()).Returns([teamId1, teamId2]);
        mockMatch.Setup(m => m.GetResultOf(teamId1)).Returns(MatchResultType.Win);
        mockMatch.Setup(m => m.GetResultOf(teamId2)).Returns(MatchResultType.Loss);

        standing.ApplyMatch(mockMatch.Object);

        var rank1 = standing.GetRank(teamId1);
        var rank2 = standing.GetRank(teamId2);

        (rank1 < rank2).Should().BeTrue();
    }

    [Fact]
    public void Enumerator_ShouldReturnSortedRows()
    {
        var teamId1 = TeamId.New().ToReference();
        var teamId2 = TeamId.New().ToReference();
        var standing = new Standing([teamId1, teamId2]);

        var mockMatch = new Mock<IMatch>();
        mockMatch.Setup(m => m.HasResult()).Returns(true);
        mockMatch.Setup(m => m.GetTeams()).Returns([teamId1, teamId2]);
        mockMatch.Setup(m => m.GetResultOf(teamId1)).Returns(MatchResultType.Win);
        mockMatch.Setup(m => m.GetResultOf(teamId2)).Returns(MatchResultType.Loss);

        standing.ApplyMatch(mockMatch.Object);

        var rows = standing.ToList();
        rows.Should().HaveCount(2);
        rows.First().Team.Should().Be(teamId1);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        var teamId1 = TeamId.New().ToReference();
        var teamId2 = TeamId.New().ToReference();
        var standing = new Standing([teamId1, teamId2]);

        var mockMatch = new Mock<IMatch>();
        mockMatch.Setup(static m => m.HasResult()).Returns(true);
        mockMatch.Setup(static m => m.GetTeams()).Returns([teamId1, teamId2]);
        mockMatch.Setup(static m => m.GetResultOf(It.IsAny<TeamReference>())).Returns(MatchResultType.Win);

        standing.ApplyMatch(mockMatch.Object);

        var str = standing.ToString();
        str.Should().Contain("1 :");
        str.Should().Contain(teamId1.ToString()).And.Contain(teamId2.ToString());
    }
}
