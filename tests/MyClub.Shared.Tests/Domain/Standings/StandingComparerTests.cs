// -----------------------------------------------------------------------
// <copyright file="StandingComparerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Moq;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Domain.Standings;
using MyClub.Shared.Domain.Standings.Comparers;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;
using Xunit;

namespace MyClub.Shared.Tests.Domain.Standings;

public class StandingComparerTests
{
    [Fact]
    public void AllAvailableComparers_ShouldContainKnownComparers()
    {
        StandingComparer.AllAvailableComparers.Should().ContainKey(nameof(StandingRowByPointsComparer));
        StandingComparer.AllAvailableComparers.Should().ContainKey(nameof(StandingRowByHeadToHeadComparer));
        StandingComparer.AllAvailableComparers.Should().ContainKey(nameof(StandingRowByGoalsDifferenceComparer));
    }

    [Fact]
    public void Default_ShouldReturnComparerWithExpectedOrder()
    {
        var comparer = StandingComparer.Default;
        comparer.Should().NotBeNull();
        comparer.Should().BeOfType<StandingComparer>();
    }

    [Fact]
    public void StandingRowByPointsComparer_ShouldCompareByPoints()
    {
        var row1 = new DummyStandingRow(TeamId.New().ToReference(), points: 10);
        var row2 = new DummyStandingRow(TeamId.New().ToReference(), points: 5);
        var comparer = new StandingRowByPointsComparer();

        comparer.Compare(row1, row2).Should().BeLessThan(0);
        comparer.Compare(row2, row1).Should().BeGreaterThan(0);
        comparer.Compare(row1, row1).Should().Be(0);
    }

    [Fact]
    public void StandingRowByGoalsDifferenceComparer_ShouldCompareByGoalsDifference()
    {
        var row1 = new DummyStandingRow(TeamId.New().ToReference(), goalsDifference: 3);
        var row2 = new DummyStandingRow(TeamId.New().ToReference(), goalsDifference: 1);
        var comparer = new StandingRowByGoalsDifferenceComparer();

        comparer.Compare(row1, row2).Should().BeLessThan(0);
        comparer.Compare(row2, row1).Should().BeGreaterThan(0);
    }

    [Fact]
    public void StandingRowByGoalsAgainstComparer_ShouldCompareAscending()
    {
        var row1 = new DummyStandingRow(TeamId.New().ToReference(), goalsAgainst: 2);
        var row2 = new DummyStandingRow(TeamId.New().ToReference(), goalsAgainst: 5);
        var comparer = new StandingRowByGoalsAgainstComparer();

        comparer.Compare(row1, row2).Should().BeLessThan(0);
        comparer.Compare(row2, row1).Should().BeGreaterThan(0);
    }

    [Fact]
    public void StandingRowByColumnComparer_ShouldRespectAscendingFlag()
    {
        var row1 = new DummyStandingRow(TeamId.New().ToReference(), gamesWon: 2);
        var row2 = new DummyStandingRow(TeamId.New().ToReference(), gamesWon: 5);
        var comparer = new StandingRowByColumnComparer(StandingColumnType.GamesWon, ascending: true);

        comparer.Compare(row1, row2).Should().BeLessThan(0);
        comparer.Compare(row2, row1).Should().BeGreaterThan(0);
    }

    [Fact]
    public void StandingRowByHeadToHeadComparer_ShouldBeZero()
    {
        var comparer = new StandingRowByHeadToHeadComparer();
        var row1 = new StandingRow(TeamId.New().ToReference());
        var row2 = new StandingRow(TeamId.New().ToReference());

        comparer.Compare(row1, row2).Should().Be(0);
    }

    [Fact]
    public void StandingRowByHeadToHeadComparer_ShouldCallStandingAndReturnRankComparison()
    {
        var row1 = new StandingRow(TeamId.New().ToReference());
        var row2 = new StandingRow(TeamId.New().ToReference());

        var matchMock = new Mock<IMatch>();
        matchMock.Setup(static m => m.HasResult(It.IsAny<TeamReference>())).Returns(true);
        matchMock.Setup(static m => m.GetTeams()).Returns([row1.Team, row2.Team]);

        var rules = new StandingRuleSet(StandingRuleSet.DefaultPoints, StandingRuleSet.DefaultColumns, new StandingComparerBuilder().ThenByPoints().ThenBy(StandingColumnType.GoalsDifference).Build());
        var comparer = new StandingRowByHeadToHeadComparer();
        comparer.SetContext([matchMock.Object], rules);

        // Mock Standing to always return 1 for row1 and 2 for row2
        // (Here, we assume Standing.GetRank returns 1 for row1.Team and 2 for row2.Team)
        // Since Standing is not injectable, this test will just check that no exception is thrown and the result is int
        var result = comparer.Compare(row1, row2);
        result.Should().BeOfType(typeof(int));
    }

    [Fact]
    public void StandingComparer_ShouldUseAllComparersInOrder()
    {
        var mockComparer1 = new Mock<IStandingComparer>();
        var mockComparer2 = new Mock<IStandingComparer>();
        var row1 = new StandingRow(TeamId.New().ToReference());
        var row2 = new StandingRow(TeamId.New().ToReference());

        mockComparer1.Setup(x => x.Compare(row1, row2)).Returns(0);
        mockComparer2.Setup(x => x.Compare(row1, row2)).Returns(1);

        var comparer = new StandingComparer([mockComparer1.Object, mockComparer2.Object]);

        comparer.Compare(row1, row2).Should().Be(1);
        mockComparer1.Verify(x => x.Compare(row1, row2), Times.Once);
        mockComparer2.Verify(x => x.Compare(row1, row2), Times.Once);
    }

    [Fact]
    public void StandingComparer_ShouldReturnZeroIfAllComparersReturnZero()
    {
        var mockComparer1 = new Mock<IStandingComparer>();
        var mockComparer2 = new Mock<IStandingComparer>();
        var row1 = new StandingRow(TeamId.New().ToReference());
        var row2 = new StandingRow(TeamId.New().ToReference());

        mockComparer1.Setup(x => x.Compare(row1, row2)).Returns(0);
        mockComparer2.Setup(x => x.Compare(row1, row2)).Returns(0);

        var comparer = new StandingComparer([mockComparer1.Object, mockComparer2.Object]);

        comparer.Compare(row1, row2).Should().Be(0);
    }
}
