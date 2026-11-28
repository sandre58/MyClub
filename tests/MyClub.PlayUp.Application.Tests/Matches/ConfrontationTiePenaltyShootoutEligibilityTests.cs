// -----------------------------------------------------------------------
// <copyright file="ConfrontationTiePenaltyShootoutEligibilityTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Matches;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Matches;

public sealed class ConfrontationTiePenaltyShootoutEligibilityTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 28, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Allows_unequal_play_score_when_two_leg_aggregate_is_tied_after_final_leg()
    {
        var (stage, first, second) = CreateTwoLegCup();
        first.Start(_clock);
        first.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        second.Start(_clock);

        var allows = ConfrontationTiePenaltyShootoutEligibility.AllowsUnequalPlayScore(
            stage,
            second,
            new Score(1, 0),
            extraTimePlayed: true,
            new PenaltyShootoutScore(5, 4),
            [first, second]);

        allows.Should().BeTrue();
    }

    private (Stage Stage, Match First, Match Second) CreateTwoLegCup()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.TwoLegs, aggregateScoring: true), _clock);
        stage.AddSlot("A");
        stage.AddSlot("B");
        stage.ReplaceBracketPairs([new BracketPair("P1", "A", "B")]);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, "A", "B", "P1");
        var home = EntryId.New();
        var away = EntryId.New();
        var first = Match.Create(stage.CompetitionId, stage.Id, home, away, _clock);
        var second = Match.Create(stage.CompetitionId, stage.Id, away, home, _clock);
        stage.AttachMatch(fixture.Id, first.Id, legIndex: 1, _clock);
        stage.AttachMatch(fixture.Id, second.Id, legIndex: 2, _clock);
        return (stage, first, second);
    }
}
