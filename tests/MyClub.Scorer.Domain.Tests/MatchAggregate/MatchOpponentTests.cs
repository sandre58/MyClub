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
using MyClub.Tests.Common;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.MatchAggregate;

public class MatchOpponentTests : TestBase
{
    [Fact]
    public void AddGoal_ShouldAddGoalToEventsAndIncreaseScore()
    {
        var team = TeamId.New().ToReference();
        var matchOpponent = new MatchOpponent(team);

        var goal = matchOpponent.AddGoal(10);

        goal.Should().NotBeNull();
        matchOpponent.Events.Should().Contain(goal);
        matchOpponent.GetGoals().Should().Contain(goal);
        matchOpponent.GetScore().Should().Be(1);
    }

    [Fact]
    public void RemoveLastGoal_ShouldRemoveLastGoal()
    {
        var team = TeamId.New().ToReference();
        var matchOpponent = new MatchOpponent(team);

        var goal1 = matchOpponent.AddGoal(5);
        var goal2 = matchOpponent.AddGoal(10);

        matchOpponent.RemoveLastGoal();

        matchOpponent.Events.Should().NotContain(goal2);
        matchOpponent.Events.Should().Contain(goal1);
        matchOpponent.GetGoals().Should().Contain(goal1);
        matchOpponent.GetScore().Should().Be(1);
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
    public void SetCards_ShouldReplaceCardsInEvents()
    {
        var team = TeamId.New().ToReference();
        var matchOpponent = new MatchOpponent(team);

        var card1 = Card.Create(CardColor.Yellow);
        var card2 = Card.Create(CardColor.Red);

        matchOpponent.AddCard(card1);
        matchOpponent.SetCards([card2]);

        matchOpponent.Events.Should().NotContain(card1);
        matchOpponent.Events.Should().Contain(card2);
        matchOpponent.GetCards().Should().Contain(card2);
        matchOpponent.GetCards().Should().HaveCount(1);
    }

    [Fact]
    public void SetScore_ShouldSetGoalsAndShootoutScore()
    {
        var team = TeamId.New().ToReference();
        var matchOpponent = new MatchOpponent(team);

        matchOpponent.SetScore(2, 3);

        matchOpponent.GetScore().Should().Be(2);
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

        matchOpponent.GetScore().Should().Be(2);
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
        matchOpponent.Events.Should().Contain(card);
        matchOpponent.GetCards().Should().Contain(card);
        matchOpponent.GetCards().Should().HaveCount(1);
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
        matchOpponent.Events.Should().BeEmpty();
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
        matchOpponent.Events.Should().BeEmpty();
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
