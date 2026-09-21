// -----------------------------------------------------------------------
// <copyright file="StageSlotsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class StageSlotsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 9, 17, 0, 0, TimeSpan.Zero));

    [Fact]
    public void AddSlot_creates_slot_with_null_entry()
    {
        var stage = CreateCup();

        var slot = stage.AddSlot("QF1-A");

        slot.SlotKey.Should().Be("QF1-A");
        slot.EntryId.Should().BeNull();
        stage.Slots.Should().ContainSingle();
    }

    [Fact]
    public void AddSlot_rejects_empty_key()
    {
        var stage = CreateCup();

        var act = () => stage.AddSlot("  ");

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SlotKeyInvalid);
    }

    [Fact]
    public void AddSlot_rejects_duplicate_key()
    {
        var stage = CreateCup();
        stage.AddSlot("QF1-A");

        var act = () => stage.AddSlot("QF1-A");

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DuplicateSlotKey);
    }

    [Fact]
    public void AddSlot_rejects_key_longer_than_max()
    {
        var stage = CreateCup();
        var tooLong = new string('X', Slot.SlotKeyMaxLength + 1);

        var act = () => stage.AddSlot(tooLong);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SlotKeyInvalid);
    }

    [Fact]
    public void AssignEntryToSlot_writes_direct_and_entry()
    {
        var stage = CreateCup();
        stage.AddSlot("QF1-A");
        var entryId = Admit(stage, EntryId.New());

        stage.AssignEntryToSlot("QF1-A", entryId);

        stage.DirectAssignments.Should().ContainSingle()
            .Which.Should().Be(new DirectAssignment("QF1-A", entryId));
        stage.FindSlot("QF1-A")!.EntryId.Should().Be(entryId);
    }

    [Fact]
    public void AssignEntryToSlot_replaces_existing_direct()
    {
        var stage = CreateCup();
        stage.AddSlot("QF1-A");
        var first = Admit(stage, EntryId.New());
        var second = Admit(stage, EntryId.New());
        stage.AssignEntryToSlot("QF1-A", first);

        stage.AssignEntryToSlot("QF1-A", second);

        stage.DirectAssignments.Should().ContainSingle().Which.EntryId.Should().Be(second);
        stage.FindSlot("QF1-A")!.EntryId.Should().Be(second);
    }

    [Fact]
    public void AssignEntryToSlot_rejects_entry_not_in_population()
    {
        var stage = CreateCup();
        stage.AddSlot("QF1-A");

        var act = () => stage.AssignEntryToSlot("QF1-A", EntryId.New());

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.EntryNotInPopulation);
    }

    [Fact]
    public void AssignEntryToSlot_rejects_duplicate_entry_on_another_slot()
    {
        var stage = CreateCup();
        stage.AddSlot("A");
        stage.AddSlot("B");
        var entryId = Admit(stage, EntryId.New());
        stage.AssignEntryToSlot("A", entryId);

        var act = () => stage.AssignEntryToSlot("B", entryId);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DuplicateEntry);
    }

    [Fact]
    public void AssignEntryToSlot_rejects_when_progression_feeds_slot()
    {
        var stage = CreateCup();
        var round = stage.AddRound("QF", _clock);
        stage.AddSlot("SF1-A");
        var fixture = stage.AddFixture(round.Id, _clock);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);
        var entryId = Admit(stage, EntryId.New());

        var act = () => stage.AssignEntryToSlot("SF1-A", entryId);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SlotFeedConflict);
    }

    [Fact]
    public void ClearSlotAssignment_clears_direct_and_entry()
    {
        var stage = CreateCup();
        stage.AddSlot("QF1-A");
        var entryId = Admit(stage, EntryId.New());
        stage.AssignEntryToSlot("QF1-A", entryId);

        stage.ClearSlotAssignment("QF1-A");

        stage.DirectAssignments.Should().BeEmpty();
        stage.FindSlot("QF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void ClearSlotAssignment_is_noop_when_no_direct_assignment()
    {
        var stage = CreateCup();
        stage.AddSlot("QF1-A");

        stage.ClearSlotAssignment("QF1-A");

        stage.DirectAssignments.Should().BeEmpty();
        stage.FindSlot("QF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void RemoveSlot_blocked_when_direct_assignment_exists()
    {
        var stage = CreateCup();
        stage.AddSlot("QF1-A");
        var entryId = Admit(stage, EntryId.New());
        stage.AssignEntryToSlot("QF1-A", entryId);

        var act = () => stage.RemoveSlot("QF1-A");

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SlotReferenced);
    }

    [Fact]
    public void RemoveSlot_blocked_when_referenced_by_local_progression()
    {
        var stage = CreateCup();
        var round = stage.AddRound("QF", _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        stage.AddSlot("SF1-A");
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);

        var act = () => stage.RemoveSlot("SF1-A");

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SlotReferenced);
    }

    [Fact]
    public void AddSlot_demotes_ready_to_draft()
    {
        var stage = CreateCup();
        stage.AddRound("QF", _clock);
        stage.Prepare(_clock);
        stage.Status.Should().Be(StageStatus.Ready);

        stage.AddSlot("QF1-A");

        stage.Status.Should().Be(StageStatus.Draft);
    }

    [Fact]
    public void Slot_mutations_locked_when_running()
    {
        var stage = CreateCup();
        stage.AddRound("QF", _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => stage.AddSlot("QF1-A");

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
    }

    private EntryId Admit(Stage stage, EntryId entryId)
    {
        var existing = stage.CompositionEntries.Select(e => e.EntryId).ToList();
        existing.Add(entryId);
        stage.ReplaceCompositionEntries(existing, _clock);
        return entryId;
    }

    private Stage CreateCup() =>
        Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
}
