// -----------------------------------------------------------------------
// <copyright file="ApplyProgressionOutcomeRcPre1Tests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// RC-PRE1 — Path/Intent snapshot stable across Materialize; Apply resolves via Fixture.BracketPairKey.
/// </summary>
public sealed class ApplyProgressionOutcomeRcPre1Tests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 24, 22, 0, 0, TimeSpan.Zero));

    [Fact]
    public void RC_PRE1_Save_on_pairs_Materialize_Apply_via_fixtureId_leaves_Path_and_Intent_unchanged()
    {
        var competition = CreateCompetition.Execute("RC-PRE1", _clock);
        var source = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var destination = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        competition.AddStage(source.Id, _clock);
        competition.AddStage(destination.Id, _clock);

        source.AddRound("Tour", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        source.AddSlot("S1");
        source.AddSlot("S2");
        source.AddSlot("S3");
        source.AddSlot("S4");
        source.SeedEntryRoundBracketPairs();
        source.Rounds[0].Fixtures.Should().BeEmpty();

        ReplaceStageProgressionRules.Execute(
            source,
            [
                new ProgressionIntentSpec(
                    IntentId: null,
                    Order: 1,
                    RoundId: source.Rounds[0].Id,
                    Outcome: ProgressionOutcome.Winner,
                    DestinationStageId: destination.Id)
            ],
            _clock);

        var snapshotPaths = source.Regulation.ProgressionRules!.Paths.Select(p => p.Copy()).ToArray();
        var snapshotIntents = source.Regulation.ProgressionRules.Intents.Select(i => i.Copy()).ToArray();
        snapshotPaths.Select(p => p.SourcePairKey).Should().Equal("P1", "P2");

        var home = EntryId.New();
        var away = EntryId.New();
        source.ApplyResolvedEntry("S1", home, _clock);
        source.ApplyResolvedEntry("S2", away, _clock);
        var materialized = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            source,
            ["P1"],
            [],
            _clock);
        var fixture = source.FindFixtureByBracketPairKey("P1")!;
        fixture.BracketPairKey.Should().Be("P1");
        var match = materialized.CreatedMatches.Should().ContainSingle().Subject;
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);

        source.Regulation.ProgressionRules!.Paths.Should().BeEquivalentTo(snapshotPaths);
        source.Regulation.ProgressionRules.Intents.Should().BeEquivalentTo(snapshotIntents);

        var results = ApplyProgressionOutcome.Execute(
            source,
            fixture.Id,
            [match],
            [source, destination],
            _clock);

        results.Should().ContainSingle();
        results[0].EntryId.Should().Be(home);
        destination.CompositionEntries.Select(e => e.EntryId).Should().Equal(home);

        source.Regulation.ProgressionRules!.Paths.Should().BeEquivalentTo(snapshotPaths);
        source.Regulation.ProgressionRules.Intents.Should().BeEquivalentTo(snapshotIntents);
    }

    [Fact]
    public void Apply_on_Cup_fixture_without_BracketPairKey_fails_closed()
    {
        var competition = CreateCompetition.Execute("RC-PRE1-fail", _clock);
        var source = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var destination = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        competition.AddStage(source.Id, _clock);
        competition.AddStage(destination.Id, _clock);

        source.AddRound("Tour", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        source.AddSlot("S1");
        source.AddSlot("S2");

        // Fixture first (no PairKey), then pairs — Cup with unbound fixture.
        var orphan = source.AddFixture(source.Rounds[0].Id, _clock);
        source.SeedEntryRoundBracketPairs();
        ReplaceStageProgressionRules.Execute(
            source,
            [
                new ProgressionIntentSpec(
                    IntentId: null,
                    Order: 1,
                    RoundId: source.Rounds[0].Id,
                    Outcome: ProgressionOutcome.Winner,
                    DestinationStageId: destination.Id)
            ],
            _clock);

        var match = Match.Create(competition.Id, source.Id, EntryId.New(), EntryId.New(), _clock);
        source.AttachMatch(orphan.Id, match.Id, legIndex: 1, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);

        var act = () => ApplyProgressionOutcome.Execute(
            source,
            orphan.Id,
            [match],
            [source, destination],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.FixtureInvalid);
    }
}
