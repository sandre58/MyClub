// -----------------------------------------------------------------------
// <copyright file="TwoLegAggregateStillLevelTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

/// <summary>
/// Parity cases aligned with <see cref="FixtureOutcomeResolver"/> two-leg aggregate scenarios.
/// </summary>
public sealed class TwoLegAggregateStillLevelTests
{
    private readonly EntryId _entryA = EntryId.New();
    private readonly EntryId _entryB = EntryId.New();

    private static TieFormat TwoLeg(bool awayGoals = false) =>
        new(
            TieFormat.TwoLegs,
            aggregateScoring: true,
            awayGoalsRule: awayGoals ? new AwayGoalsRule() : null);

    [Fact]
    public void AfterPlayScores_aggregate_leader_is_not_still_level()
    {
        var legs = Legs(
            Leg(1, _entryA, _entryB, new Score(2, 0)),
            Leg(2, _entryB, _entryA, new Score(1, 0)));

        TwoLegAggregateStillLevel.AfterPlayScores(TwoLeg(), _entryA, _entryB, legs).Should().BeFalse();
        TwoLegAggregateStillLevel.Evaluate(TwoLeg(), _entryA, _entryB, legs)
            .Should().Be(TwoLegAggregatePlayDecision.EntryAWins);
    }

    [Fact]
    public void AfterPlayScores_tied_aggregate_without_away_goals_is_still_level()
    {
        var legs = Legs(
            Leg(1, _entryA, _entryB, new Score(1, 0)),
            Leg(2, _entryB, _entryA, new Score(1, 0)));

        TwoLegAggregateStillLevel.AfterPlayScores(TwoLeg(awayGoals: false), _entryA, _entryB, legs).Should().BeTrue();
        TwoLegAggregateStillLevel.Evaluate(TwoLeg(awayGoals: false), _entryA, _entryB, legs)
            .Should().Be(TwoLegAggregatePlayDecision.StillLevel);
    }

    [Fact]
    public void AfterPlayScores_away_goals_break_prevents_still_level_when_aggregate_tied()
    {
        var legs = Legs(
            Leg(1, _entryA, _entryB, new Score(2, 1)),
            Leg(2, _entryB, _entryA, new Score(1, 0)));

        TwoLegAggregateStillLevel.AfterPlayScores(TwoLeg(awayGoals: true), _entryA, _entryB, legs).Should().BeFalse();
        TwoLegAggregateStillLevel.Evaluate(TwoLeg(awayGoals: true), _entryA, _entryB, legs)
            .Should().Be(TwoLegAggregatePlayDecision.EntryBWins);
    }

    [Fact]
    public void AfterPlayScores_same_scores_without_away_goals_rule_remain_still_level()
    {
        var legs = Legs(
            Leg(1, _entryA, _entryB, new Score(2, 1)),
            Leg(2, _entryB, _entryA, new Score(1, 0)));

        TwoLegAggregateStillLevel.AfterPlayScores(TwoLeg(awayGoals: false), _entryA, _entryB, legs).Should().BeTrue();
    }

    private static FixtureLegSnapshot Leg(
        int index,
        EntryId home,
        EntryId away,
        Score score) =>
        new(index, MatchId.New(), home, away, MatchStatus.Finished, score, extraTimePlayed: false);

    private static FixtureLegSnapshot[] Legs(params FixtureLegSnapshot[] legs) => legs;
}