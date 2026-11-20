// -----------------------------------------------------------------------
// <copyright file="AssignEntryToSlotTests.cs" company="Stéphane ANDRE">
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

/// <summary>
/// Manual Cup placement — AssignEntryToSlot / ClearSlotAssignment Application wrappers.
/// </summary>
public sealed class AssignEntryToSlotTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 24, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assign_then_clear_round_trips_direct_assignment()
    {
        var (stage, entryId) = SeedCupWithPopulation();

        AssignEntryToSlot.Execute(stage, "S1", entryId, expectedEntryId: null)
            .Should().BeTrue();

        stage.DirectAssignments.Should().ContainSingle(a =>
            a.SlotKey == "S1" && a.EntryId.Equals(entryId));
        stage.FindSlot("S1")!.EntryId.Should().Be(entryId);

        ClearSlotAssignment.Execute(stage, "S1", entryId).Should().BeTrue();

        stage.DirectAssignments.Should().BeEmpty();
        stage.FindSlot("S1")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Assign_rejects_when_stage_running()
    {
        var (stage, entryId) = SeedCupWithPopulation();
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => AssignEntryToSlot.Execute(stage, "S1", entryId, expectedEntryId: null);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    [Fact]
    public void Clear_rejects_when_stage_running()
    {
        var (stage, entryId) = SeedCupWithPopulation();
        AssignEntryToSlot.Execute(stage, "S1", entryId, expectedEntryId: null);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => ClearSlotAssignment.Execute(stage, "S1", entryId);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    [Fact]
    public void Assign_rejects_empty_slot_key()
    {
        var (stage, entryId) = SeedCupWithPopulation();

        var act = () => AssignEntryToSlot.Execute(stage, "  ", entryId, expectedEntryId: null);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.InvalidStructureIntent);
    }

    [Fact]
    public void Assign_propagates_entry_not_in_population()
    {
        var (stage, _) = SeedCupWithPopulation();
        var outsider = EntryId.New();

        var act = () => AssignEntryToSlot.Execute(stage, "S1", outsider, expectedEntryId: null);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.EntryNotInPopulation);
    }

    [Fact]
    public void Clear_is_noop_when_no_direct_assignment()
    {
        var (stage, _) = SeedCupWithPopulation();
        stage.ApplyResolvedEntry("S1", stage.CompositionEntries[0].EntryId, _clock);

        var occupant = stage.FindSlot("S1")!.EntryId;
        ClearSlotAssignment.Execute(stage, "S1", occupant).Should().BeFalse();

        stage.DirectAssignments.Should().BeEmpty();
        stage.FindSlot("S1")!.EntryId.Should().NotBeNull();
    }

    [Fact]
    public void Assign_refuses_when_materialized_pair_would_diverge()
    {
        var (stage, entryA, entryB, match) = SeedMaterializedCupPair();
        var other = stage.CompositionEntries.First(e => !e.EntryId.Equals(entryA) && !e.EntryId.Equals(entryB)).EntryId;

        var act = () => AssignEntryToSlot.Execute(stage, "S1", other, entryA, [match]);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MaterializedOpponentConflict);
        stage.FindSlot("S1")!.EntryId.Should().Be(entryA);
        stage.FindSlot("S2")!.EntryId.Should().Be(entryB);
    }

    [Fact]
    public void Clear_refuses_when_materialized_pair_would_diverge()
    {
        var (stage, entryA, entryB, match) = SeedMaterializedCupPair();
        stage.AssignEntryToSlot(
            "S1",
            entryA,
            MaterializedParticipantSnapshots.FromMatches([match]));

        var act = () => ClearSlotAssignment.Execute(stage, "S1", entryA, [match]);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MaterializedOpponentConflict);
        stage.FindSlot("S1")!.EntryId.Should().Be(entryA);
        stage.FindSlot("S2")!.EntryId.Should().Be(entryB);
    }

    [Fact]
    public void Assign_with_current_expected_mutates()
    {
        var (stage, entryId, other) = SeedCupWithTwoEntries();

        AssignEntryToSlot.Execute(stage, "S1", entryId, expectedEntryId: null).Should().BeTrue();
        AssignEntryToSlot.Execute(stage, "S1", other, entryId).Should().BeTrue();

        stage.FindSlot("S1")!.EntryId.Should().Be(other);
    }

    [Fact]
    public void Assign_with_stale_expected_refuses_and_keeps_occupant()
    {
        var (stage, entryId, other) = SeedCupWithTwoEntries();
        AssignEntryToSlot.Execute(stage, "S1", entryId, expectedEntryId: null);

        var act = () => AssignEntryToSlot.Execute(stage, "S1", other, expectedEntryId: null);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.SlotAssignmentStale);
        stage.FindSlot("S1")!.EntryId.Should().Be(entryId);
    }

    [Fact]
    public void Assign_null_expected_on_occupied_slot_is_stale()
    {
        var (stage, entryId) = SeedCupWithPopulation();
        AssignEntryToSlot.Execute(stage, "S1", entryId, expectedEntryId: null);

        var act = () => AssignEntryToSlot.Execute(stage, "S1", entryId, expectedEntryId: null);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.SlotAssignmentStale);
        stage.FindSlot("S1")!.EntryId.Should().Be(entryId);
    }

    [Fact]
    public void Assign_same_occupant_is_noop()
    {
        var (stage, entryId) = SeedCupWithPopulation();
        AssignEntryToSlot.Execute(stage, "S1", entryId, expectedEntryId: null);

        AssignEntryToSlot.Execute(stage, "S1", entryId, entryId).Should().BeFalse();

        stage.DirectAssignments.Should().ContainSingle();
        stage.FindSlot("S1")!.EntryId.Should().Be(entryId);
    }

    [Fact]
    public void Clear_free_slot_with_null_expected_is_noop()
    {
        var (stage, _) = SeedCupWithPopulation();

        ClearSlotAssignment.Execute(stage, "S1", expectedEntryId: null).Should().BeFalse();

        stage.FindSlot("S1")!.EntryId.Should().BeNull();
        stage.DirectAssignments.Should().BeEmpty();
    }

    [Fact]
    public void Stale_expected_on_applied_draw_slot_is_refused_before_the_draw_gate()
    {
        var (stage, protectedSlot, occupant, other) = SeedAppliedDrawSlot();

        var assign = () => AssignEntryToSlot.Execute(stage, protectedSlot, other, expectedEntryId: null);
        var clear = () => ClearSlotAssignment.Execute(stage, protectedSlot, expectedEntryId: null);

        assign.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.SlotAssignmentStale);
        clear.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.SlotAssignmentStale);
        stage.FindSlot(protectedSlot)!.EntryId.Should().Be(occupant);
    }

    private (Stage Stage, EntryId EntryId) SeedCupWithPopulation()
    {
        var competition = CreateCompetition.Execute("Cup-Manual", _clock);
        var stage = AddCompetitionStage.Execute(
            competition,
            StructureIntent.Cup(2, "KO"),
            _clock);
        var entry = competition.AddEntry(TeamId.New(), "Team A", _clock);
        ReplaceStageAffectationAuthoring.Execute(stage, competition, [entry.Id], _clock);
        return (stage, entry.Id);
    }

    private (Stage Stage, EntryId First, EntryId Second) SeedCupWithTwoEntries()
    {
        var competition = CreateCompetition.Execute("Cup-Manual-2", _clock);
        var stage = AddCompetitionStage.Execute(
            competition,
            StructureIntent.Cup(2, "KO"),
            _clock);
        var first = competition.AddEntry(TeamId.New(), "Team A", _clock);
        var second = competition.AddEntry(TeamId.New(), "Team B", _clock);
        ReplaceStageAffectationAuthoring.Execute(stage, competition, [first.Id, second.Id], _clock);
        return (stage, first.Id, second.Id);
    }

    private (Stage Stage, string ProtectedSlot, EntryId Occupant, EntryId Other) SeedAppliedDrawSlot()
    {
        var competition = CreateCompetition.Execute("Cup-Stale-Gate", _clock);
        for (var i = 0; i < 4; i++)
        {
            AddEntry.Execute(competition, $"T{i}", _clock);
        }

        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(4), _clock).Stage;
        var entryIds = competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ReplaceAffectationAuthoring(entryIds, _clock);
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot));
        var slotTargets = stage.Slots.Select(slot => slot.SlotKey).ToArray();
        GenerateDrawResolution.Execute(stage, draw.Id, _clock, slotTargets).IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        var protectedSlot = draw.Resolution.SlotResults[0].SlotKey;
        var occupant = draw.Resolution.SlotResults[0].EntryId;
        var other = entryIds.First(id => !id.Equals(occupant));
        return (stage, protectedSlot, occupant, other);
    }

    private (Stage Stage, EntryId EntryA, EntryId EntryB, Match Match) SeedMaterializedCupPair()
    {
        var competition = CreateCompetition.Execute("Cup-F1", _clock);
        var stage = AddCompetitionStage.Execute(
            competition,
            StructureIntent.Cup(4, "KO"),
            _clock);
        var entryA = competition.AddEntry(TeamId.New(), "Team A", _clock);
        var entryB = competition.AddEntry(TeamId.New(), "Team B", _clock);
        var entryC = competition.AddEntry(TeamId.New(), "Team C", _clock);
        ReplaceStageAffectationAuthoring.Execute(
            stage,
            competition,
            [entryA.Id, entryB.Id, entryC.Id],
            _clock);

        stage.SeedEntryRoundBracketPairs();
        var round = stage.AddRound("Tour", _clock);
        stage.ApplyResolvedEntry("S1", entryA.Id, _clock);
        stage.ApplyResolvedEntry("S2", entryB.Id, _clock);

        var fixture = stage.AddFixture(round.Id, _clock, "S1", "S2", "P1");
        var match = Match.Create(competition.Id, stage.Id, entryA.Id, entryB.Id, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);

        return (stage, entryA.Id, entryB.Id, match);
    }
}
