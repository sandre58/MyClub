// -----------------------------------------------------------------------
// <copyright file="MaterializeCupFromOccupiedSlotsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application;
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
            [new CupSlotPair("SF1-A", "SF1-B")],
            [],
            _clock);

        result.CreatedMatches.Should().HaveCount(1);
        result.AlreadyComplete.Should().BeFalse();
        result.CreatedMatches[0].HomeEntryId.Should().Be(a);
        result.CreatedMatches[0].AwayEntryId.Should().Be(b);

        var fixture = stage.Rounds[0].Fixtures.Should().ContainSingle().Subject;
        fixture.SlotAKey.Should().Be("SF1-A");
        fixture.SlotBKey.Should().Be("SF1-B");
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
            [new CupSlotPair("SF1-A", "SF1-B")],
            [],
            _clock);

        result.CreatedMatches.Should().HaveCount(2);
        result.CreatedMatches[0].HomeEntryId.Should().Be(a);
        result.CreatedMatches[0].AwayEntryId.Should().Be(b);
        result.CreatedMatches[1].HomeEntryId.Should().Be(b);
        result.CreatedMatches[1].AwayEntryId.Should().Be(a);
        stage.Rounds[0].Fixtures.Should().ContainSingle();
        stage.Rounds[0].Fixtures[0].Attachments.Should().HaveCount(2);
    }

    [Fact]
    public void Execute_second_call_exact_is_noop()
    {
        var competition = CreateCompetition.Execute("Cup-C2-Idem", _clock);
        var stage = CreateSemiStage(competition.Id);
        var a = EntryId.New();
        var b = EntryId.New();
        stage.ApplyResolvedEntry("SF1-A", a, _clock);
        stage.ApplyResolvedEntry("SF1-B", b, _clock);

        var first = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            [new CupSlotPair("SF1-A", "SF1-B")],
            [],
            _clock);
        var second = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            [new CupSlotPair("SF1-A", "SF1-B")],
            first.CreatedMatches,
            _clock);

        second.CreatedMatches.Should().BeEmpty();
        second.AlreadyComplete.Should().BeTrue();
        second.AttachedMatchIds.Should().HaveCount(1);
    }

    [Fact]
    public void Execute_rejects_partial_leg1_only_for_two_legs()
    {
        var competition = CreateCompetition.Execute("Cup-C2-Partial", _clock);
        var stage = CreateSemiStage(
            competition.Id,
            new TieFormat(TieFormat.TwoLegs, aggregateScoring: true));
        var a = EntryId.New();
        var b = EntryId.New();
        stage.ApplyResolvedEntry("SF1-A", a, _clock);
        stage.ApplyResolvedEntry("SF1-B", b, _clock);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, "SF1-A", "SF1-B");
        var leg1 = Match.Create(competition.Id, stage.Id, a, b, _clock);
        stage.AttachMatch(fixture.Id, leg1.Id, legIndex: 1, _clock);

        var act = () => MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            [new CupSlotPair("SF1-A", "SF1-B")],
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
            [new CupSlotPair("SF1-A", "SF1-B")],
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
            [new CupSlotPair("SF1-A", "SF1-B")],
            [],
            _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            [new CupSlotPair("SF1-A", "SF1-B")],
            [],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.OrganisationNotMutable);
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
        qf.AddSlot("QF1-A", _clock);
        qf.AddSlot("QF1-B", _clock);
        qf.ApplyResolvedEntry("QF1-A", home, _clock);
        qf.ApplyResolvedEntry("QF1-B", away, _clock);
        var qfFixture = qf.AddFixture(qfRound.Id, _clock, "QF1-A", "QF1-B");
        var qfMatch = Match.Create(competition.Id, qf.Id, home, away, _clock);
        qf.AttachMatch(qfFixture.Id, qfMatch.Id, legIndex: 1, _clock);
        qfMatch.Start(_clock);
        qfMatch.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        qf.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    qfFixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(sf.Id, "SF1-A")),
                new ProgressionPath(
                    qfFixture.Id,
                    ProgressionOutcome.Loser,
                    new ProgressionDestination(sf.Id, "SF1-B"))
            ]),
            _clock);

        ApplyProgressionOutcome.Execute(qf, qfFixture.Id, [qfMatch], [qf, sf], _clock);
        sf.FindSlot("SF1-A")!.EntryId.Should().Be(home);
        sf.FindSlot("SF1-B")!.EntryId.Should().Be(away);

        var result = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            sf,
            [new CupSlotPair("SF1-A", "SF1-B")],
            [],
            _clock);

        result.CreatedMatches.Should().HaveCount(1);
        result.CreatedMatches[0].HomeEntryId.Should().Be(home);
        result.CreatedMatches[0].AwayEntryId.Should().Be(away);
        sf.Rounds[0].Fixtures.Should().ContainSingle();
    }

    private Stage CreateSemiStage(CompetitionId competitionId, TieFormat? tieFormat = null)
    {
        var stage = Stage.Create(competitionId, new StageName("SF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("Semi-Finals", tieFormat, _clock);
        stage.AddSlot("SF1-A", _clock);
        stage.AddSlot("SF1-B", _clock);
        stage.AddSlot("SF2-A", _clock);
        stage.AddSlot("SF2-B", _clock);
        return stage;
    }
}
