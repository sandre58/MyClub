// -----------------------------------------------------------------------
// <copyright file="ApplyDrawTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Stages.Events;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class ApplyDrawTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 11, 14, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void Execute_rejects_missing_draw()
    {
        var stage = CreateStage();

        var act = () => ApplyDraw.Execute(stage, DrawId.New(), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawNotFound);
    }

    [Fact]
    public void Execute_rejects_draft_draw()
    {
        var stage = CreateStage();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        var entry = EntryId.New();
        stage.AddSlot("A");
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry, "A")]),
            _clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.FindSlot("A")!.EntryId.Should().BeNull();
        stage.DomainEvents.Should().BeEmpty();
        draw.Status.Should().Be(DrawStatus.Draft);
    }

    [Fact]
    public void Execute_rejects_cancelled_draw()
    {
        var stage = CreateStage();
        var (draw, _) = PublishSlotDraw(stage, "A");
        stage.CancelDraw(draw.Id, _clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.FindSlot("A")!.EntryId.Should().BeNull();
        stage.DomainEvents.Should().BeEmpty();
        draw.Status.Should().Be(DrawStatus.Cancelled);
    }

    [Fact]
    public void Execute_rejects_not_resolved_draw()
    {
        var stage = CreateStage();
        stage.AddSlot("A");
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([EntryId.New()]));
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.FindSlot("A")!.EntryId.Should().BeNull();
        stage.DomainEvents.Should().BeEmpty();
        draw.Resolution.State.Should().Be(DrawResolutionState.NotResolved);
    }

    [Fact]
    public void Execute_rejects_no_solution_draw()
    {
        var stage = CreateStage();
        stage.AddSlot("A");
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([EntryId.New()]));
        stage.MarkDrawNoSolution(draw.Id, _clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.FindSlot("A")!.EntryId.Should().BeNull();
        stage.DomainEvents.Should().BeEmpty();
        draw.Resolution.State.Should().Be(DrawResolutionState.NoSolution);
        draw.Status.Should().Be(DrawStatus.Draft);
    }

    [Fact]
    public void Execute_slot_applies_resolution()
    {
        var stage = CreateStage();
        var (draw, entry) = PublishSlotDraw(stage, "A");

        var result = ApplyDraw.Execute(stage, draw.Id, _clock);

        result.SlotInstructions.Should().ContainSingle();
        result.SlotInstructions[0].EntryId.Should().Be(entry);
        result.CreatedMatches.Should().BeEmpty();
        stage.FindSlot("A")!.EntryId.Should().Be(entry);
        draw.Status.Should().Be(DrawStatus.Published);
        draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
    }

    [Fact]
    public void Execute_slot_idempotent_when_already_conform()
    {
        var stage = CreateStage();
        var (draw, entry) = PublishSlotDraw(stage, "A");
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.ClearDomainEvents();

        var result = ApplyDraw.Execute(stage, draw.Id, _clock);

        result.SlotInstructions.Should().ContainSingle();
        stage.FindSlot("A")!.EntryId.Should().Be(entry);
        stage.DomainEvents.Should().NotContain(e => e is StageSlotOccupantChanged);
    }

    [Fact]
    public void Execute_slot_applies_when_stage_running()
    {
        var stage = CreateStage();
        stage.AddRound("R1", _clock);
        var (draw, entry) = PublishSlotDraw(stage, "A");
        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.ClearDomainEvents();

        var result = ApplyDraw.Execute(stage, draw.Id, _clock);

        result.SlotInstructions.Should().ContainSingle();
        stage.Status.Should().Be(StageStatus.Running);
        stage.FindSlot("A")!.EntryId.Should().Be(entry);
        draw.Status.Should().Be(DrawStatus.Published);
    }

    [Fact]
    public void Execute_slot_applies_when_stage_suspended()
    {
        var stage = CreateStage();
        stage.AddRound("R1", _clock);
        var (draw, entry) = PublishSlotDraw(stage, "A");
        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.Suspend(_clock);
        stage.ClearDomainEvents();

        var result = ApplyDraw.Execute(stage, draw.Id, _clock);

        result.SlotInstructions.Should().ContainSingle();
        stage.Status.Should().Be(StageStatus.Suspended);
        stage.FindSlot("A")!.EntryId.Should().Be(entry);
        draw.Status.Should().Be(DrawStatus.Published);
    }

    [Fact]
    public void Execute_slot_rejects_when_stage_completed()
    {
        var stage = CreateStage();
        stage.AddRound("R1", _clock);
        var (draw, _) = PublishSlotDraw(stage, "A");
        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.Complete(_clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
        stage.FindSlot("A")!.EntryId.Should().BeNull();
        stage.DomainEvents.Should().BeEmpty();
        draw.Status.Should().Be(DrawStatus.Published);
        stage.Status.Should().Be(StageStatus.Completed);
    }

    [Fact]
    public void Execute_slot_rejects_divergent_occupant()
    {
        var stage = CreateStage();
        var (draw, entry) = PublishSlotDraw(stage, "A");
        var other = EntryId.New();
        stage.ApplyResolvedEntry("A", other, _clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.FindSlot("A")!.EntryId.Should().Be(other);
        stage.DomainEvents.Should().BeEmpty();
        _ = entry;
    }

    [Fact]
    public void Execute_slot_rejects_missing_slot_without_mutation()
    {
        var stage = CreateStage();
        var entry = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry, "Missing")]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        stage.AddSlot("A");
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.FindSlot("A")!.EntryId.Should().BeNull();
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Execute_slot_rejects_direct_assignment_conflict()
    {
        var stage = CreateStage();
        var (draw, entry) = PublishSlotDraw(stage, "A");
        var direct = EntryId.New();
        stage.ReplaceCompositionEntries([direct], _clock);
        stage.AssignEntryToSlot("A", direct);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.FindSlot("A")!.EntryId.Should().Be(direct);
        stage.DomainEvents.Should().BeEmpty();
        _ = entry;
    }

    [Fact]
    public void Execute_slot_preflight_failure_mutates_nothing_on_second_slot()
    {
        var stage = CreateStage();
        stage.AddSlot("A");
        stage.AddSlot("B");
        var e1 = EntryId.New();
        var e2 = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([e1, e2]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(e1, "A"),
                new SlotDrawPlacement(e2, "B")
            ]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        stage.ApplyResolvedEntry("B", EntryId.New(), _clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.FindSlot("A")!.EntryId.Should().BeNull();
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Execute_group_applies_resolution()
    {
        var stage = CreateStage();
        var group = stage.AddGroup("A", _clock);
        var entry = EntryId.New();
        var draw = PublishGroupDraw(stage, entry, group.Id);

        var result = ApplyDraw.Execute(stage, draw.Id, _clock);

        result.SlotInstructions.Should().BeEmpty();
        result.CreatedMatches.Should().BeEmpty();
        group.EntryIds.Should().ContainSingle().Which.Should().Be(entry);
        draw.Status.Should().Be(DrawStatus.Published);
        draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
    }

    [Fact]
    public void Execute_group_idempotent_when_already_conform()
    {
        var stage = CreateStage();
        var group = stage.AddGroup("A", _clock);
        var entry = EntryId.New();
        var draw = PublishGroupDraw(stage, entry, group.Id);
        ApplyDraw.Execute(stage, draw.Id, _clock);

        var result = ApplyDraw.Execute(stage, draw.Id, _clock);

        result.SlotInstructions.Should().BeEmpty();
        group.EntryIds.Should().ContainSingle().Which.Should().Be(entry);
    }

    [Fact]
    public void Execute_group_rematerializes_entry_from_other_group()
    {
        var stage = CreateStage();
        var groupA = stage.AddGroup("A", _clock);
        var groupB = stage.AddGroup("B", _clock);
        var entry = EntryId.New();
        var draw = PublishGroupDraw(stage, entry, groupA.Id);
        stage.AssignEntryToGroup(groupB.Id, entry);

        var result = ApplyDraw.Execute(stage, draw.Id, _clock);

        result.SlotInstructions.Should().BeEmpty();
        groupA.EntryIds.Should().ContainSingle().Which.Should().Be(entry);
        groupB.EntryIds.Should().BeEmpty();
    }

    [Fact]
    public void Execute_group_rematerialize_leaves_unrelated_entries()
    {
        var stage = CreateStage();
        var groupA = stage.AddGroup("A", _clock);
        var groupB = stage.AddGroup("B", _clock);
        var drawn = EntryId.New();
        var outsider = EntryId.New();
        stage.AssignEntryToGroup(groupB.Id, outsider);
        var draw = PublishGroupDraw(stage, drawn, groupA.Id);
        stage.AssignEntryToGroup(groupB.Id, drawn);

        ApplyDraw.Execute(stage, draw.Id, _clock);

        groupA.EntryIds.Should().ContainSingle().Which.Should().Be(drawn);
        groupB.EntryIds.Should().ContainSingle().Which.Should().Be(outsider);
    }

    [Fact]
    public void Execute_group_rejects_missing_group_without_mutation()
    {
        var stage = CreateStage();
        var existing = stage.AddGroup("A", _clock);
        var entry = EntryId.New();
        var missingGroupId = GroupId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForGroup([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedGroups([new GroupDrawPlacement(entry, missingGroupId)]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        existing.EntryIds.Should().BeEmpty();
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Execute_group_rejects_when_structure_locked()
    {
        var stage = CreateStage();
        var group = stage.AddGroup("A", _clock);
        var prepareEntry = EntryId.New();
        stage.AssignEntryToGroup(group.Id, prepareEntry);
        stage.AddMatchday(1, _clock);

        var entry = EntryId.New();
        var draw = PublishGroupDraw(stage, entry, group.Id);

        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        group.EntryIds.Should().Equal(prepareEntry);
        stage.DomainEvents.Should().BeEmpty();
        draw.Status.Should().Be(DrawStatus.Published);
    }

    [Fact]
    public void Execute_group_rejects_when_stage_suspended()
    {
        var stage = CreateStage();
        var group = stage.AddGroup("A", _clock);
        var prepareEntry = EntryId.New();
        stage.AssignEntryToGroup(group.Id, prepareEntry);
        stage.AddMatchday(1, _clock);

        var entry = EntryId.New();
        var draw = PublishGroupDraw(stage, entry, group.Id);

        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.Suspend(_clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        group.EntryIds.Should().Equal(prepareEntry);
        stage.DomainEvents.Should().BeEmpty();
        draw.Status.Should().Be(DrawStatus.Published);
        stage.Status.Should().Be(StageStatus.Suspended);
    }

    private Stage CreateStage() =>
        Stage.Create(_competitionId, new StageName("Phase"), SampleRegulations.Standard(), _clock);

    private (Draw Draw, EntryId Entry) PublishSlotDraw(Stage stage, string slotKey)
    {
        stage.AddSlot(slotKey);
        var entry = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry, slotKey)]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        return (draw, entry);
    }

    private Draw PublishGroupDraw(Stage stage, EntryId entry, GroupId groupId)
    {
        var draw = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForGroup([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedGroups([new GroupDrawPlacement(entry, groupId)]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        return draw;
    }
}
