// -----------------------------------------------------------------------
// <copyright file="MatchOpponentTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.MatchAggregate.MatchEvents;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Teams;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.MatchAggregate;

public class MatchOpponentTests
{
    [Fact]
    public void AddGoal_ShouldAddGoalToEventsAndIncreaseScore()
    {
        var team = TeamId.New().ToReference();
        var matchOpponent = new MatchOpponent(team);

        var goal = matchOpponent.AddGoal(10);

        goal.Should().NotBeNull();
        matchOpponent.Goals.Should().Contain(goal);
        matchOpponent.GetEvents().Should().Contain(goal);
        matchOpponent.Score.Should().Be(1);
    }

    [Fact]
    public void RemoveLastGoal_ShouldRemoveLastGoal()
    {
        var team = TeamId.New().ToReference();
        var matchOpponent = new MatchOpponent(team);

        var goal1 = matchOpponent.AddGoal(5);
        var goal2 = matchOpponent.AddGoal(10);

        matchOpponent.RemoveLastGoal();

        matchOpponent.Goals.Should().NotContain(goal2);
        matchOpponent.Goals.Should().Contain(goal1);
        matchOpponent.GetEvents().Should().Contain(goal1);
        matchOpponent.Score.Should().Be(1);
    }

    [Fact]
    public void AddPenaltyShootout_ShouldAddPenaltyShootoutAndIncreaseShootoutScore()
    {
        var team = TeamId.New().ToReference();
        var matchOpponent = new MatchOpponent(team);

        var shootout = matchOpponent.AddPenaltyShootout(PenaltyShootoutOutcome.Succeeded);

        shootout.Should().NotBeNull();
        matchOpponent.Shootout.Should().Contain(shootout);
        matchOpponent.GetShootoutScore().Should().Be(1);
    }

    [Fact]
    public void RemoveLastSucceededPenaltyShootout_ShouldRemoveLastSucceededShootout()
    {
        var team = TeamId.New().ToReference();
        var matchOpponent = new MatchOpponent(team);

        var s1 = matchOpponent.AddPenaltyShootout(PenaltyShootoutOutcome.Failed);
        var s2 = matchOpponent.AddPenaltyShootout(PenaltyShootoutOutcome.Succeeded);
        var s3 = matchOpponent.AddPenaltyShootout(PenaltyShootoutOutcome.Succeeded);

        matchOpponent.RemoveLastSucceededPenaltyShootout();

        matchOpponent.Shootout.Should().NotContain(s3);
        matchOpponent.Shootout.Should().Contain(s2);
        matchOpponent.Shootout.Should().Contain(s1);
        matchOpponent.GetShootoutScore().Should().Be(1);
    }

    [Fact]
    public void SetScore_ShouldSetGoalsAndShootoutScore()
    {
        var team = TeamId.New().ToReference();
        var matchOpponent = new MatchOpponent(team);

        matchOpponent.SetScore(2, 3);

        matchOpponent.
        Score.Should().Be(2);
        matchOpponent.GetShootoutScore().Should().Be(3);
    }

    [Fact]
    public void SetScore_WithGoalsAndShootouts_ShouldSetCorrectly()
    {
        var team = TeamId.New().ToReference();
        var matchOpponent = new MatchOpponent(team);

        var goals = new[] { Goal.Create(GoalType.Regular), Goal.Create(GoalType.Regular) };
        var shootouts = new[] { PenaltyShootout.Create(result: PenaltyShootoutOutcome.Succeeded) };

        matchOpponent.SetScore(goals, shootouts);

        matchOpponent.
        Score.Should().Be(2);
        matchOpponent.GetShootoutScore().Should().Be(1);
    }

    [Fact]
    public void AddCard_ShouldAddCardToEvents()
    {
        var team = TeamId.New().ToReference();
        var matchOpponent = new MatchOpponent(team);

        var card = Card.Create(CardColor.Yellow);

        var added = matchOpponent.AddCard(card);

        added.Should().Be(card);
        matchOpponent.Cards.Should().Contain(card);
        matchOpponent.GetEvents().Should().Contain(card);
        matchOpponent.GetEvents().Should().HaveCount(1);
    }

    [Fact]
    public void DoWithdraw_ShouldSetIsWithdrawnAndClearEvents()
    {
        var team = TeamId.New().ToReference();
        var matchOpponent = new MatchOpponent(team);

        matchOpponent.AddGoal();
        matchOpponent.AddPenaltyShootout(PenaltyShootoutOutcome.Succeeded);

        matchOpponent.DoWithdraw();

        matchOpponent.IsWithdrawn.Should().BeTrue();
        matchOpponent.Goals.Should().BeEmpty();
        matchOpponent.Shootout.Should().BeEmpty();
    }

    [Fact]
    public void Reset_ShouldClearEventsAndShootoutAndUnsetWithdrawn()
    {
        var team = TeamId.New().ToReference();
        var matchOpponent = new MatchOpponent(team);

        matchOpponent.AddGoal();
        matchOpponent.AddPenaltyShootout(PenaltyShootoutOutcome.Succeeded);
        matchOpponent.DoWithdraw();

        matchOpponent.Reset();

        matchOpponent.IsWithdrawn.Should().BeFalse();
        matchOpponent.Goals.Should().BeEmpty();
        matchOpponent.Shootout.Should().BeEmpty();
    }

    [Fact]
    public void ToString_ShouldReturnTeamIdString()
    {
        var team = TeamId.New().ToReference();
        var matchOpponent = new MatchOpponent(team);

        matchOpponent.ToString().Should().Be(team.ToString());
    }
}
