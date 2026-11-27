// -----------------------------------------------------------------------
// <copyright file="TieFormatProgressionWithStoredDefaultTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class TieFormatProgressionWithStoredDefaultTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 27, 21, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Prepare_and_progression_accept_one_leg_draw_resolved_with_default_tab()
    {
        var competitionId = CompetitionId.New();
        var regulation = new StageRegulation(
            SampleRegulations.Standard().MatchRules,
            standingRules: null,
            new TieFormat(TieFormat.SingleLeg, false, penaltyShootoutRule: new PenaltyShootoutRule()));
        var stage = Stage.Create(competitionId, new StageName("Cup"), regulation, _clock);
        var round = stage.AddRound("R1", tieFormat: null, _clock);
        stage.AddSlot("KO-A");
        stage.AddSlot("KO-B");
        stage.AddSlot("SF1-A");
        const string pairKey = "P1";
        stage.ReplaceBracketPairs([new BracketPair(pairKey, "KO-A", "KO-B")]);
        var home = EntryId.New();
        var away = EntryId.New();
        var fixture = stage.AddFixture(round.Id, _clock, "KO-A", "KO-B", pairKey);
        var match = Match.Create(competitionId, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        match.Start(_clock);
        match.Finish(
            new MatchResult(
                ResultType.Played,
                new Score(1, 1),
                extraTimePlayed: false,
                new PenaltyShootoutScore(5, 4)),
            _clock);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    pairKey,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);

        PrepareStage.Execute(stage, [stage], _clock);

        var results = ApplyProgressionOutcome.Execute(stage, fixture.Id, [match], [stage], _clock);
        results.Should().ContainSingle();
        stage.FindSlot("SF1-A")!.EntryId.Should().Be(home);
    }
}
