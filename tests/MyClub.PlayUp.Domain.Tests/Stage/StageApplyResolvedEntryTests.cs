// -----------------------------------------------------------------------
// <copyright file="StageApplyResolvedEntryTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stage;
using MyClub.PlayUp.Domain.Stage.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;

namespace MyClub.PlayUp.Domain.Tests.Stage;

public sealed class StageApplyResolvedEntryTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 9, 19, 0, 0, TimeSpan.Zero));

    [Fact]
    public void ApplyResolvedEntry_sets_entry_on_vacant_slot()
    {
        var stage = CreateCupWithSlots("A");
        var entryId = EntryId.New();
        stage.ClearDomainEvents();

        stage.ApplyResolvedEntry("A", entryId, _clock);

        stage.FindSlot("A")!.EntryId.Should().Be(entryId);
        stage.DirectAssignments.Should().BeEmpty();
        var changed = stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageSlotOccupantChanged>().Subject;
        changed.StageId.Should().Be(stage.Id);
        changed.SlotKey.Should().Be("A");
        changed.PreviousEntryId.Should().BeNull();
        changed.EntryId.Should().Be(entryId);
    }

    [Fact]
    public void ApplyResolvedEntry_replaces_existing_occupant()
    {
        var stage = CreateCupWithSlots("A");
        var first = EntryId.New();
        var second = EntryId.New();
        stage.ApplyResolvedEntry("A", first, _clock);
        stage.ClearDomainEvents();

        stage.ApplyResolvedEntry("A", second, _clock);

        stage.FindSlot("A")!.EntryId.Should().Be(second);
        var changed = stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageSlotOccupantChanged>().Subject;
        changed.PreviousEntryId.Should().Be(first);
        changed.EntryId.Should().Be(second);
    }

    [Fact]
    public void ApplyResolvedEntry_same_entry_is_noop()
    {
        var stage = CreateCupWithSlots("A");
        var entryId = EntryId.New();
        stage.ApplyResolvedEntry("A", entryId, _clock);
        stage.ClearDomainEvents();

        stage.ApplyResolvedEntry("A", entryId, _clock);

        stage.FindSlot("A")!.EntryId.Should().Be(entryId);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ApplyResolvedEntry_moves_entry_from_another_slot_without_second_event()
    {
        var stage = CreateCupWithSlots("A", "B");
        var entryId = EntryId.New();
        stage.ApplyResolvedEntry("A", entryId, _clock);
        stage.ClearDomainEvents();

        stage.ApplyResolvedEntry("B", entryId, _clock);

        stage.FindSlot("A")!.EntryId.Should().BeNull();
        stage.FindSlot("B")!.EntryId.Should().Be(entryId);
        var changed = stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageSlotOccupantChanged>().Subject;
        changed.SlotKey.Should().Be("B");
        changed.PreviousEntryId.Should().BeNull();
        changed.EntryId.Should().Be(entryId);
    }

    [Fact]
    public void ApplyResolvedEntry_rejects_when_direct_assignment_present()
    {
        var stage = CreateCupWithSlots("A");
        var directEntry = EntryId.New();
        stage.AssignEntryToSlot("A", directEntry, _clock);
        var directSnapshot = stage.DirectAssignments.ToArray();

        var act = () => stage.ApplyResolvedEntry("A", EntryId.New(), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SlotFeedConflict);
        stage.DirectAssignments.Should().Equal(directSnapshot);
        stage.FindSlot("A")!.EntryId.Should().Be(directEntry);
    }

    [Fact]
    public void ClearResolvedEntry_clears_dynamic_occupant()
    {
        var stage = CreateCupWithSlots("A");
        var entryId = EntryId.New();
        stage.ApplyResolvedEntry("A", entryId, _clock);
        stage.ClearDomainEvents();

        stage.ClearResolvedEntry("A", _clock);

        stage.FindSlot("A")!.EntryId.Should().BeNull();
        var changed = stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageSlotOccupantChanged>().Subject;
        changed.PreviousEntryId.Should().Be(entryId);
        changed.EntryId.Should().BeNull();
    }

    [Fact]
    public void ClearResolvedEntry_vacant_is_noop()
    {
        var stage = CreateCupWithSlots("A");
        stage.ClearDomainEvents();

        stage.ClearResolvedEntry("A", _clock);

        stage.FindSlot("A")!.EntryId.Should().BeNull();
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ApplyResolvedEntry_does_not_create_direct_assignment()
    {
        var stage = CreateCupWithSlots("A");

        stage.ApplyResolvedEntry("A", EntryId.New(), _clock);

        stage.DirectAssignments.Should().BeEmpty();
    }

    [Fact]
    public void ApplyResolvedEntry_rejects_unknown_slot()
    {
        var stage = CreateCupWithSlots("A");

        var act = () => stage.ApplyResolvedEntry("missing", EntryId.New(), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SlotNotFound);
    }

    [Fact]
    public void ApplyResolvedEntry_allowed_in_draft()
    {
        var stage = CreateCupWithSlots("A");
        stage.Status.Should().Be(StageStatus.Draft);

        stage.ApplyResolvedEntry("A", EntryId.New(), _clock);

        stage.Status.Should().Be(StageStatus.Draft);
        stage.FindSlot("A")!.EntryId.Should().NotBeNull();
    }

    [Fact]
    public void ApplyResolvedEntry_allowed_in_ready_without_demote()
    {
        var stage = CreateCupWithSlots("A");
        stage.AddRound("QF", _clock);
        stage.Prepare(_clock);
        stage.Status.Should().Be(StageStatus.Ready);
        stage.ClearDomainEvents();

        stage.ApplyResolvedEntry("A", EntryId.New(), _clock);

        stage.Status.Should().Be(StageStatus.Ready);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageSlotOccupantChanged>();
    }

    [Fact]
    public void ApplyResolvedEntry_allowed_in_running()
    {
        var stage = CreateRunningWithSlots("A");
        stage.ClearDomainEvents();

        stage.ApplyResolvedEntry("A", EntryId.New(), _clock);

        stage.Status.Should().Be(StageStatus.Running);
        stage.FindSlot("A")!.EntryId.Should().NotBeNull();
    }

    [Fact]
    public void ApplyResolvedEntry_allowed_in_suspended()
    {
        var stage = CreateRunningWithSlots("A");
        stage.Suspend(_clock);
        stage.ClearDomainEvents();

        stage.ApplyResolvedEntry("A", EntryId.New(), _clock);

        stage.Status.Should().Be(StageStatus.Suspended);
        stage.FindSlot("A")!.EntryId.Should().NotBeNull();
    }

    [Fact]
    public void ApplyResolvedEntry_rejects_when_completed()
    {
        var stage = CreateRunningWithSlots("A");
        stage.Complete(_clock);

        var act = () => stage.ApplyResolvedEntry("A", EntryId.New(), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
        stage.Status.Should().Be(StageStatus.Completed);
        stage.FindSlot("A")!.EntryId.Should().BeNull();
    }

    private StageAggregate CreateCupWithSlots(params string[] slotKeys)
    {
        var stage = StageAggregate.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);

        foreach (var key in slotKeys)
        {
            stage.AddSlot(key, _clock);
        }

        return stage;
    }

    private StageAggregate CreateRunningWithSlots(params string[] slotKeys)
    {
        var stage = CreateCupWithSlots(slotKeys);
        stage.AddRound("QF", _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        return stage;
    }
}
