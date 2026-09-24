// -----------------------------------------------------------------------
// <copyright file="MaterializeCupFromOccupiedSlotsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class MaterializeCupFromOccupiedSlotsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_creates_positional_fixture_and_single_leg_match()
    {
        var competition = CreateCompetition.Execute("Cup-C2", _clock);
        var stage = CreateSemiStage(competition.Id);
        var a = EntryId.New();
        var b = EntryId.New();
        stage.ApplyResolvedEntry("SF1-A", a, _clock);
        stage.ApplyResolvedEntry("SF1-B", b, _clock);

        var result = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["P1"],
            [],
            _clock);

        result.CreatedMatches.Should().HaveCount(1);
        result.AlreadyComplete.Should().BeFalse();
        result.CreatedMatches[0].HomeEntryId.Should().Be(a);
        result.CreatedMatches[0].AwayEntryId.Should().Be(b);

        var fixture = stage.Rounds[0].Fixtures.Should().ContainSingle().Subject;
        fixture.SlotAKey.Should().Be("SF1-A");
        fixture.SlotBKey.Should().Be("SF1-B");
        fixture.BracketPairKey.Should().Be("P1");
        fixture.Attachments.Should().ContainSingle().Which.LegIndex.Should().Be(1);
    }

    [Fact]
    public void Execute_two_legs_creates_mirrored_matches_on_same_fixture()
    {
        var competition = CreateCompetition.Execute("Cup-C2-2L", _clock);
        var stage = CreateSemiStage(
            competition.Id,
            new TieFormat(TieFormat.TwoLegs, aggregateScoring: true));
        var a = EntryId.New();
        var b = EntryId.New();
        stage.ApplyResolvedEntry("SF1-A", a, _clock);
        stage.ApplyResolvedEntry("SF1-B", b, _clock);

        var result = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["P1"],
            [],
            _clock);

        result.CreatedMatches.Should().HaveCount(2);
        result.CreatedMatches[0].HomeEntryId.Should().Be(a);
        result.CreatedMatches[0].AwayEntryId.Should().Be(b);
        result.CreatedMatches[1].HomeEntryId.Should().Be(b);
        result.CreatedMatches[1].AwayEntryId.Should().Be(a);
        stage.Rounds[0].Fixtures.Should().ContainSingle();
        stage.Rounds[0].Fixtures[0].BracketPairKey.Should().Be("P1");
        stage.Rounds[0].Fixtures[0].Attachments.Should().HaveCount(2);
    }

    [Fact]
    public void Execute_omit_materializes_only_eligible_pairs()
    {
        var competition = CreateCompetition.Execute("Cup-C2-Partial-Eligible", _clock);
        var stage = CreateSemiStage(competition.Id);
        stage.ApplyResolvedEntry("SF1-A", EntryId.New(), _clock);
        stage.ApplyResolvedEntry("SF1-B", EntryId.New(), _clock);

        // P2 slots vacant → not eligible
        var result = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            null,
            [],
            _clock);

        result.CreatedMatches.Should().HaveCount(1);
        stage.Rounds[0].Fixtures.Should().ContainSingle().Which.BracketPairKey.Should().Be("P1");
        stage.FindFixtureByBracketPairKey("P2").Should().BeNull();
    }

    [Fact]
    public void Execute_rejects_identical_occupants()
    {
        var competition = CreateCompetition.Execute("Cup-C2-Same", _clock);
        var stage = CreateSemiStage(competition.Id);
        var same = EntryId.New();
        stage.ApplyResolvedEntry("SF1-A", same, _clock);
        stage.ApplyResolvedEntry("SF1-B", same, _clock);

        var act = () => MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["P1"],
            [],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.MaterializationFailure);
        stage.Rounds[0].Fixtures.Should().BeEmpty();
    }

    [Fact]
    public void Execute_omit_when_all_pairs_materialized_is_already_complete()
    {
        var competition = CreateCompetition.Execute("Cup-C2-Idem", _clock);
        var stage = CreateSemiStage(competition.Id);
        var a = EntryId.New();
        var b = EntryId.New();
        stage.ApplyResolvedEntry("SF1-A", a, _clock);
        stage.ApplyResolvedEntry("SF1-B", b, _clock);
        stage.ApplyResolvedEntry("SF2-A", EntryId.New(), _clock);
        stage.ApplyResolvedEntry("SF2-B", EntryId.New(), _clock);

        MaterializeCupFromOccupiedSlots.Execute(competition, stage, null, [], _clock);
        var second = MaterializeCupFromOccupiedSlots.Execute(competition, stage, null, [], _clock);

        second.CreatedMatches.Should().BeEmpty();
        second.AlreadyComplete.Should().BeTrue();
        second.AttachedMatchIds.Should().BeEmpty();
    }

    [Fact]
    public void Execute_explicit_already_materialized_pair_fails()
    {
        var competition = CreateCompetition.Execute("Cup-C2-Explicit-Fail", _clock);
        var stage = CreateSemiStage(competition.Id);
        stage.ApplyResolvedEntry("SF1-A", EntryId.New(), _clock);
        stage.ApplyResolvedEntry("SF1-B", EntryId.New(), _clock);

        MaterializeCupFromOccupiedSlots.Execute(competition, stage, ["P1"], [], _clock);

        var act = () => MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["P1"],
            [],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.MaterializationFailure);
    }

    [Fact]
    public void Execute_rejects_partial_legs_when_fixture_bound_to_pair()
    {
        var competition = CreateCompetition.Execute("Cup-C2-Partial", _clock);
        var stage = CreateSemiStage(
            competition.Id,
            new TieFormat(TieFormat.TwoLegs, aggregateScoring: true));
        var a = EntryId.New();
        var b = EntryId.New();
        stage.ApplyResolvedEntry("SF1-A", a, _clock);
        stage.ApplyResolvedEntry("SF1-B", b, _clock);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, "SF1-A", "SF1-B", "P1");
        var leg1 = Match.Create(competition.Id, stage.Id, a, b, _clock);
        stage.AttachMatch(fixture.Id, leg1.Id, legIndex: 1, _clock);

        var act = () => MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["P1"],
            [leg1],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.MaterializationFailure);
    }

    [Fact]
    public void Execute_rejects_empty_slot()
    {
        var competition = CreateCompetition.Execute("Cup-C2-Empty", _clock);
        var stage = CreateSemiStage(competition.Id);
        stage.ApplyResolvedEntry("SF1-A", EntryId.New(), _clock);

        var act = () => MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["P1"],
            [],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.MaterializationFailure);
    }

    [Fact]
    public void Execute_rejects_unknown_pair_key()
    {
        var competition = CreateCompetition.Execute("Cup-C2-Unknown", _clock);
        var stage = CreateSemiStage(competition.Id);
        stage.ApplyResolvedEntry("SF1-A", EntryId.New(), _clock);
        stage.ApplyResolvedEntry("SF1-B", EntryId.New(), _clock);

        var act = () => MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["PX"],
            [],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.MaterializationFailure);
    }

    [Fact]
    public void Execute_rejects_when_stage_running()
    {
        var competition = CreateCompetition.Execute("Cup-C2-Run", _clock);
        var stage = CreateSemiStage(competition.Id);
        var a = EntryId.New();
        var b = EntryId.New();
        stage.ApplyResolvedEntry("SF1-A", a, _clock);
        stage.ApplyResolvedEntry("SF1-B", b, _clock);
        MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["P1"],
            [],
            _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["P1"],
            [],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    [Fact]
    public void Execute_allows_when_competition_running_and_stage_draft()
    {
        var competition = CreateCompetition.Execute("Cup-C2-Late", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddStage(StageId.New(), _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var stage = CreateSemiStage(competition.Id);
        var a = EntryId.New();
        var b = EntryId.New();
        stage.ApplyResolvedEntry("SF1-A", a, _clock);
        stage.ApplyResolvedEntry("SF1-B", b, _clock);

        var result = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["P1"],
            [],
            _clock);

        result.CreatedMatches.Should().HaveCount(1);
        result.AlreadyComplete.Should().BeFalse();
    }

    [Fact]
    public void Execute_rejects_when_competition_suspended()
    {
        var competition = CreateCompetition.Execute("Cup-C2-Susp", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddStage(StageId.New(), _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        competition.Suspend(_clock);

        var stage = CreateSemiStage(competition.Id);
        stage.ApplyResolvedEntry("SF1-A", EntryId.New(), _clock);
        stage.ApplyResolvedEntry("SF1-B", EntryId.New(), _clock);

        var act = () => MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["P1"],
            [],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    [Fact]
    public void Composition_progression_then_materialize_from_slots()
    {
        var competition = CreateCompetition.Execute("Cup-C2-Comp", _clock);
        var qf = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var sf = CreateSemiStage(competition.Id);
        var home = EntryId.New();
        var away = EntryId.New();
        var qfRound = qf.AddRound("Tour", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        qf.AddSlot("QF1-A");
        qf.AddSlot("QF1-B");
        qf.ReplaceBracketPairs([new BracketPair("P1", "QF1-A", "QF1-B")]);
        qf.ApplyResolvedEntry("QF1-A", home, _clock);
        qf.ApplyResolvedEntry("QF1-B", away, _clock);
        var qfFixture = qf.AddFixture(qfRound.Id, _clock, "QF1-A", "QF1-B", "P1");
        var qfMatch = Match.Create(competition.Id, qf.Id, home, away, _clock);
        qf.AttachMatch(qfFixture.Id, qfMatch.Id, legIndex: 1, _clock);
        qfMatch.Start(_clock);
        qfMatch.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        qf.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath("P1",
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(sf.Id)),
                new ProgressionPath("P1",
                    ProgressionOutcome.Loser,
                    ProgressionDestination.ForPopulation(sf.Id))
            ]),
            _clock);

        ApplyProgressionOutcome.Execute(qf, qfFixture.Id, [qfMatch], [qf, sf], _clock);
        sf.CompositionEntries.Select(e => e.EntryId).Should().BeEquivalentTo([home, away]);
        sf.FindSlot("SF1-A")!.EntryId.Should().BeNull();
        sf.FindSlot("SF1-B")!.EntryId.Should().BeNull();
        sf.ApplyResolvedEntry("SF1-A", home, _clock);
        sf.ApplyResolvedEntry("SF1-B", away, _clock);

        var result = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            sf,
            ["P1"],
            [],
            _clock);

        result.CreatedMatches.Should().HaveCount(1);
        result.CreatedMatches[0].HomeEntryId.Should().Be(home);
        result.CreatedMatches[0].AwayEntryId.Should().Be(away);
        sf.Rounds[0].Fixtures.Should().ContainSingle().Which.BracketPairKey.Should().Be("P1");
    }

    private Stage CreateSemiStage(CompetitionId competitionId, TieFormat? tieFormat = null)
    {
        var stage = Stage.Create(competitionId, new StageName("SF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("Semi-Finals", tieFormat, _clock);
        stage.AddSlot("SF1-A");
        stage.AddSlot("SF1-B");
        stage.AddSlot("SF2-A");
        stage.AddSlot("SF2-B");
        stage.SeedEntryRoundBracketPairs();
        return stage;
    }
}
