// -----------------------------------------------------------------------
// <copyright file="MatchTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using AutoFixture;
using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Teams;
using MyClub.Tests.Common;
using Xunit;
using Match = MyClub.Scorer.Domain.MatchAggregate.Match;

namespace MyClub.Scorer.Domain.Tests.MatchAggregate;

public class MatchTests : TestBase
{
    private Match CreateDefaultMatch()
    {
        var date = Fixture.Create<DateTime>();
        var homeId = TeamId.New().ToReference();
        var awayId = TeamId.New().ToReference();
        return Match.Create(date, homeId, awayId);
    }

    [Fact]
    public void Create_ShouldInitializeMatchWithDefaultValues()
    {
        var date = DateTime.Now;
        var homeId = TeamId.New().ToReference();
        var awayId = TeamId.New().ToReference();

        var match = Match.Create(date, homeId, awayId);

        match.OriginDate.Should().Be(date);
        match.Home.Team.Should().Be(homeId);
        match.Away.Team.Should().Be(awayId);
        match.Format.Should().Be(MatchFormat.Default);
        match.Rules.Should().Be(MatchRules.Default);
        match.Status.Should().Be(MatchStatus.None);
    }

    [Fact]
    public void Schedule_ShouldUpdateOriginDate()
    {
        var match = CreateDefaultMatch();
        var newDate = DateTime.Now.AddDays(1);

        match.Schedule(newDate);

        match.OriginDate.Should().Be(newDate);
    }

    [Fact]
    public void Schedule_WithOffset_ShouldUpdateOriginDate()
    {
        var match = CreateDefaultMatch();
        var originalDate = match.OriginDate;
        match.Schedule(2, MyNet.Utilities.Units.TimeUnit.Day);

        match.OriginDate.Should().Be(originalDate.AddDays(2));
    }

    [Fact]
    public void Postpone_ShouldSetStatusAndPostponedDate()
    {
        var match = CreateDefaultMatch();
        var postponedDate = DateTime.Now.AddDays(5);

        match.Postpone(postponedDate);

        match.Status.Should().Be(MatchStatus.Postponed);
        match.PostponedDate.Should().Be(postponedDate);
        match.Date.Should().Be(postponedDate);
    }

    [Fact]
    public void Start_ShouldSetStatusToInProgress()
    {
        var match = CreateDefaultMatch();
        match.Start();
        match.Status.Should().Be(MatchStatus.InProgress);
    }

    [Fact]
    public void Suspend_ShouldSetStatusToSuspended()
    {
        var match = CreateDefaultMatch();
        match.Suspend();
        match.Status.Should().Be(MatchStatus.Suspended);
    }

    [Fact]
    public void Played_ShouldSetStatusToPlayed()
    {
        var match = CreateDefaultMatch();
        match.Played();
        match.Status.Should().Be(MatchStatus.Played);
    }

    [Fact]
    public void Reset_ShouldClearScoresAndStatus()
    {
        var match = CreateDefaultMatch();
        match.Home.AddGoal();
        match.Away.AddGoal();
        match.Played();

        match.Reset();

        match.Status.Should().Be(MatchStatus.None);
        match.Home.GetScore().Should().Be(0);
        match.Away.GetScore().Should().Be(0);
    }

    [Fact]
    public void Cancel_ShouldSetStatusToCancelled()
    {
        var match = CreateDefaultMatch();
        match.Cancel();
        match.Status.Should().Be(MatchStatus.Cancelled);
    }

    [Fact]
    public void Invert_ShouldSwapHomeAndAway()
    {
        var match = CreateDefaultMatch();
        var homeId = match.Home.Team;
        var awayId = match.Away.Team;

        match.Invert();

        match.Home.Team.Should().Be(awayId);
        match.Away.Team.Should().Be(homeId);
    }

    [Fact]
    public void SetScore_ShouldSetGoalsAndShootoutScores()
    {
        var match = Match.Create(DateTime.Now, TeamId.New().ToReference(), TeamId.New().ToReference(), MatchFormat.NoDraw);
        match.SetScore(2, 1, true, 3, 2);

        match.Home.GetScore().Should().Be(2);
        match.Away.GetScore().Should().Be(1);
        match.Home.GetShootoutScore().Should().Be(3);
        match.Away.GetShootoutScore().Should().Be(2);
        match.AfterExtraTime.Should().BeTrue();
    }

    [Fact]
    public void SetScore_WithWithdrawn_ShouldNotSetAfterExtraTime()
    {
        var match = Match.Create(DateTime.Now, TeamId.New().ToReference(), TeamId.New().ToReference(), MatchFormat.NoDraw);
        match.Home.DoWithdraw();
        match.SetScore(2, 1, true, 3, 2);

        match.AfterExtraTime.Should().BeFalse();
    }

    [Fact]
    public void HasResult_ShouldReturnTrueWhenPlayedOrInProgressOrSuspended()
    {
        var match = CreateDefaultMatch();
        match.Played();
        match.HasResult().Should().BeTrue();

        match = CreateDefaultMatch();
        match.Start();
        match.HasResult().Should().BeTrue();

        match = CreateDefaultMatch();
        match.Suspend();
        match.HasResult().Should().BeTrue();
    }

    [Fact]
    public void IsDraw_ShouldReturnTrueIfScoresAreEqual()
    {
        var match = CreateDefaultMatch();
        match.Home.AddGoal();
        match.Away.AddGoal();
        match.Played();

        match.IsDraw().Should().BeTrue();
    }

    [Fact]
    public void GetWinnerAndLooser_ShouldReturnCorrectTeamId()
    {
        var match = CreateDefaultMatch();
        match.SetScore(2, 1);
        match.Played();

        match.GetWinner().Should().Be(match.Home.Team);
        match.GetLooser().Should().Be(match.Away.Team);
    }

    [Fact]
    public void GetExtendedResultOf_ShouldReturnWithdrawnIfOpponentIsWithdrawn()
    {
        var match = CreateDefaultMatch();
        match.Home.DoWithdraw();
        match.Played();

        match.GetResultOf(match.Home.Team).Should().Be(MatchResultType.Withdraw);
        match.GetResultOf(match.Away.Team).Should().Be(MatchResultType.Win);
    }

    [Fact]
    public void GetResultOf_ShouldReturnCorrectResult()
    {
        var match = CreateDefaultMatch();
        match.SetScore(2, 1);
        match.Played();

        match.GetOutcomeOf(match.Home.Team).Should().Be(MatchOutcome.Win);
        match.GetOutcomeOf(match.Away.Team).Should().Be(MatchOutcome.Loss);
    }

    [Fact]
    public void Participate_ShouldReturnTrueForHomeAndAway()
    {
        var match = CreateDefaultMatch();
        match.Participate(match.Home.Team).Should().BeTrue();
        match.Participate(match.Away.Team).Should().BeTrue();
    }

    [Fact]
    public void IsHomeTeamAndIsAwayTeam_ShouldReturnCorrectly()
    {
        var match = CreateDefaultMatch();
        match.IsHomeTeam(match.Home.Team).Should().BeTrue();
        match.IsAwayTeam(match.Away.Team).Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldIncludeTeamsAndScore()
    {
        var match = CreateDefaultMatch();
        match.SetScore(2, 2);
        match.Played();

        var str = match.ToString();
        str.Should().Contain(match.Home.Team.ToString());
        str.Should().Contain(match.Away.Team.ToString());
        str.Should().Contain("2-2");
    }
}
