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
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// Placement manuel Coupe — AssignEntryToSlot / ClearSlotAssignment Application wrappers.
/// </summary>
public sealed class AssignEntryToSlotTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 24, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assign_then_clear_round_trips_direct_assignment()
    {
        var (stage, entryId) = SeedCupWithPopulation();

        AssignEntryToSlot.Execute(stage, "S1", entryId);

        stage.DirectAssignments.Should().ContainSingle(a =>
            a.SlotKey == "S1" && a.EntryId.Equals(entryId));
        stage.FindSlot("S1")!.EntryId.Should().Be(entryId);

        ClearSlotAssignment.Execute(stage, "S1");

        stage.DirectAssignments.Should().BeEmpty();
        stage.FindSlot("S1")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Assign_rejects_when_stage_running()
    {
        var (stage, entryId) = SeedCupWithPopulation();
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => AssignEntryToSlot.Execute(stage, "S1", entryId);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    [Fact]
    public void Clear_rejects_when_stage_running()
    {
        var (stage, entryId) = SeedCupWithPopulation();
        AssignEntryToSlot.Execute(stage, "S1", entryId);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => ClearSlotAssignment.Execute(stage, "S1");

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    [Fact]
    public void Assign_rejects_empty_slot_key()
    {
        var (stage, entryId) = SeedCupWithPopulation();

        var act = () => AssignEntryToSlot.Execute(stage, "  ", entryId);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.InvalidStructureIntent);
    }

    [Fact]
    public void Assign_propagates_entry_not_in_population()
    {
        var (stage, _) = SeedCupWithPopulation();
        var outsider = EntryId.New();

        var act = () => AssignEntryToSlot.Execute(stage, "S1", outsider);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.EntryNotInPopulation);
    }

    [Fact]
    public void Clear_is_noop_when_no_direct_assignment()
    {
        var (stage, _) = SeedCupWithPopulation();
        stage.ApplyResolvedEntry("S1", stage.CompositionEntries[0].EntryId, _clock);

        ClearSlotAssignment.Execute(stage, "S1");

        stage.DirectAssignments.Should().BeEmpty();
        stage.FindSlot("S1")!.EntryId.Should().NotBeNull();
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
}
