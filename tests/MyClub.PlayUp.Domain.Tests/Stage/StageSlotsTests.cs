// -----------------------------------------------------------------------
// <copyright file="StageSlotsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stage;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;

namespace MyClub.PlayUp.Domain.Tests.Stage;

public sealed class StageSlotsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 9, 17, 0, 0, TimeSpan.Zero));

    [Fact]
    public void AddSlot_creates_slot_with_null_entry()
    {
        var stage = CreateCup();

        var slot = stage.AddSlot("QF1-A", _clock);

        slot.SlotKey.Should().Be("QF1-A");
        slot.EntryId.Should().BeNull();
        stage.Slots.Should().ContainSingle();
    }

    [Fact]
    public void AddSlot_rejects_empty_key()
    {
        var stage = CreateCup();

        var act = () => stage.AddSlot("  ", _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SlotKeyInvalid);
    }

    [Fact]
    public void AddSlot_rejects_duplicate_key()
    {
        var stage = CreateCup();
        stage.AddSlot("QF1-A", _clock);

        var act = () => stage.AddSlot("QF1-A", _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DuplicateSlotKey);
    }

    [Fact]
    public void AddSlot_rejects_key_longer_than_max()
    {
        var stage = CreateCup();
        var tooLong = new string('X', Slot.SlotKeyMaxLength + 1);

        var act = () => stage.AddSlot(tooLong, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SlotKeyInvalid);
    }

    [Fact]
    public void AssignEntryToSlot_writes_direct_and_entry()
    {
        var stage = CreateCup();
        stage.AddSlot("QF1-A", _clock);
        var entryId = EntryId.New();

        stage.AssignEntryToSlot("QF1-A", entryId, _clock);

        stage.DirectAssignments.Should().ContainSingle()
            .Which.Should().Be(new DirectAssignment("QF1-A", entryId));
        stage.FindSlot("QF1-A")!.EntryId.Should().Be(entryId);
    }

    [Fact]
    public void AssignEntryToSlot_replaces_existing_direct()
    {
        var stage = CreateCup();
        stage.AddSlot("QF1-A", _clock);
        var first = EntryId.New();
        var second = EntryId.New();
        stage.AssignEntryToSlot("QF1-A", first, _clock);

        stage.AssignEntryToSlot("QF1-A", second, _clock);

        stage.DirectAssignments.Should().ContainSingle().Which.EntryId.Should().Be(second);
        stage.FindSlot("QF1-A")!.EntryId.Should().Be(second);
    }

    [Fact]
    public void AssignEntryToSlot_rejects_duplicate_entry_on_another_slot()
    {
        var stage = CreateCup();
        stage.AddSlot("A", _clock);
        stage.AddSlot("B", _clock);
        var entryId = EntryId.New();
        stage.AssignEntryToSlot("A", entryId, _clock);

        var act = () => stage.AssignEntryToSlot("B", entryId, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DuplicateEntry);
    }

    [Fact]
    public void AssignEntryToSlot_rejects_when_progression_feeds_slot()
    {
        var stage = CreateCup();
        var round = stage.AddRound("QF", _clock);
        stage.AddSlot("SF1-A", _clock);
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

        var act = () => stage.AssignEntryToSlot("SF1-A", EntryId.New(), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SlotFeedConflict);
    }

    [Fact]
    public void ClearSlotAssignment_clears_direct_and_entry()
    {
        var stage = CreateCup();
        stage.AddSlot("QF1-A", _clock);
        stage.AssignEntryToSlot("QF1-A", EntryId.New(), _clock);

        stage.ClearSlotAssignment("QF1-A", _clock);

        stage.DirectAssignments.Should().BeEmpty();
        stage.FindSlot("QF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void ClearSlotAssignment_is_noop_when_no_direct_assignment()
    {
        var stage = CreateCup();
        stage.AddSlot("QF1-A", _clock);

        stage.ClearSlotAssignment("QF1-A", _clock);

        stage.DirectAssignments.Should().BeEmpty();
        stage.FindSlot("QF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void RemoveSlot_blocked_when_direct_assignment_exists()
    {
        var stage = CreateCup();
        stage.AddSlot("QF1-A", _clock);
        stage.AssignEntryToSlot("QF1-A", EntryId.New(), _clock);

        var act = () => stage.RemoveSlot("QF1-A", _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SlotReferenced);
    }

    [Fact]
    public void RemoveSlot_blocked_when_referenced_by_local_progression()
    {
        var stage = CreateCup();
        var round = stage.AddRound("QF", _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        stage.AddSlot("SF1-A", _clock);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);

        var act = () => stage.RemoveSlot("SF1-A", _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SlotReferenced);
    }

    [Fact]
    public void AddSlot_demotes_ready_to_draft()
    {
        var stage = CreateCup();
        stage.AddRound("QF", _clock);
        stage.Prepare(_clock);
        stage.Status.Should().Be(StageStatus.Ready);

        stage.AddSlot("QF1-A", _clock);

        stage.Status.Should().Be(StageStatus.Draft);
    }

    [Fact]
    public void Slot_mutations_locked_when_running()
    {
        var stage = CreateCup();
        stage.AddRound("QF", _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => stage.AddSlot("QF1-A", _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
    }

    private StageAggregate CreateCup() =>
        StageAggregate.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
}
