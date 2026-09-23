// -----------------------------------------------------------------------
// <copyright file="StageDrawTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Stages.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class StageDrawTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void CreateDraw_is_draft_not_resolved()
    {
        var stage = CreateStage();
        stage.ClearDomainEvents();

        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);

        draw.Status.Should().Be(DrawStatus.Draft);
        draw.Kind.Should().Be(DrawResolutionKind.Slot);
        draw.Resolution.State.Should().Be(DrawResolutionState.NotResolved);
        draw.Inputs.Should().BeNull();
        stage.DomainEvents.Should().ContainSingle(e => e is StageDrawCreated);
    }

    [Fact]
    public void CreateDraw_on_Ready_demotes_to_Draft()
    {
        var stage = CreateStage();
        stage.AddRound("R1", _clock);
        stage.Prepare(_clock);
        stage.Status.Should().Be(StageStatus.Ready);

        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);

        stage.Status.Should().Be(StageStatus.Draft);
        draw.Status.Should().Be(DrawStatus.Draft);
        stage.DomainEvents.Should().Contain(e => e is StageDrawCreated);
    }

    [Fact]
    public void CreateDraw_rejects_when_stage_running()
    {
        var stage = CreateStage();
        stage.AddRound("R1", _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => stage.CreateDraw(DrawResolutionKind.Slot, _clock);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
        stage.Draws.Should().BeEmpty();
        stage.Status.Should().Be(StageStatus.Running);
    }

    [Fact]
    public void ConfigureDrawInputs_is_sole_entry_point_for_pool()
    {
        var stage = CreateStage();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        var entries = new[] { EntryId.New(), EntryId.New() };

        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot(entries));

        draw.Inputs!.Entries.Should().Equal(entries);
    }

    [Fact]
    public void RecordResolution_kind_mismatch_is_rejected()
    {
        var stage = CreateStage();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        var entry = EntryId.New();
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]));
        var groupResolution = DrawResolution.ResolvedGroups(
            [new GroupDrawPlacement(entry, GroupId.New())]);

        var act = () => stage.RecordDrawResolution(draw.Id, groupResolution, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawResolutionKindMismatch);
    }

    [Fact]
    public void MarkNoSolution_then_Publish_is_rejected()
    {
        var stage = CreateStage();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([EntryId.New()]));
        stage.MarkDrawNoSolution(draw.Id, _clock);

        draw.Resolution.State.Should().Be(DrawResolutionState.NoSolution);
        var act = () => stage.PublishDraw(draw.Id, _clock);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawInvalidTransition);
        draw.Status.Should().Be(DrawStatus.Draft);
    }

    [Fact]
    public void Published_draw_is_immutable()
    {
        var stage = CreateStage();
        var (draw, _) = PublishSlotDraw(stage);

        var actConfigure = () => stage.ConfigureDrawInputs(
            draw.Id,
            DrawInputs.ForSlot([EntryId.New()]));
        var actRecord = () => stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(EntryId.New(), "A")]),
            _clock);

        actConfigure.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawImmutable);
        actRecord.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawImmutable);
    }

    [Fact]
    public void Cancel_then_Publish_is_rejected_and_rerun_uses_new_draw()
    {
        var stage = CreateStage();
        var (draw1, _) = PublishSlotDraw(stage);
        stage.CancelDraw(draw1.Id, _clock);

        draw1.Status.Should().Be(DrawStatus.Cancelled);
        var republish = () => stage.PublishDraw(draw1.Id, _clock);
        republish.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawInvalidTransition);

        var (draw2, _) = PublishSlotDraw(stage);
        draw2.Id.Should().NotBe(draw1.Id);
        draw2.Status.Should().Be(DrawStatus.Published);
    }

    [Fact]
    public void ReplaceDrawRules_clear_rejected_while_non_cancelled_draw_exists()
    {
        var stage = CreateStage();
        stage.ReplaceDrawRules(new DrawRules(DrawMode.Random), _clock);
        _ = PublishSlotDraw(stage);

        var clear = () => stage.ReplaceDrawRules(null, _clock);

        clear.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.DrawRulesClearBlockedByActiveDraw);
        stage.Regulation.DrawRules.Should().NotBeNull();
    }

    [Fact]
    public void ReplaceDrawRules_clear_allowed_after_draw_cancelled()
    {
        var stage = CreateStage();
        stage.ReplaceDrawRules(new DrawRules(DrawMode.Random), _clock);
        var (draw, _) = PublishSlotDraw(stage);
        stage.CancelDraw(draw.Id, _clock);

        stage.ReplaceDrawRules(null, _clock);

        stage.Regulation.DrawRules.Should().BeNull();
    }

    [Fact]
    public void ReplaceDrawRules_clear_allowed_when_only_draft_draw_was_cancelled()
    {
        var stage = CreateStage();
        stage.ReplaceDrawRules(new DrawRules(DrawMode.Random), _clock);
        var draft = stage.CreateDraw(DrawResolutionKind.Slot, _clock);

        var clearWhileDraft = () => stage.ReplaceDrawRules(null, _clock);
        clearWhileDraft.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.DrawRulesClearBlockedByActiveDraw);

        stage.CancelDraw(draft.Id, _clock);
        stage.ReplaceDrawRules(null, _clock);

        stage.Regulation.DrawRules.Should().BeNull();
    }

    [Fact]
    public void Fixed_slot_placements_do_not_create_DirectAssignment()
    {
        var stage = CreateStage();
        stage.AddSlot("A");
        var entry = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(
            draw.Id,
            DrawInputs.ForSlot([entry], fixedPlacements: [new SlotDrawPlacement(entry, "A")]));

        stage.DirectAssignments.Should().BeEmpty();
        draw.Inputs!.FixedSlots.Should().ContainSingle();
    }

    [Fact]
    public void ToSlotAssignmentInstructions_from_published_slot_draw()
    {
        var stage = CreateStage();
        var (draw, entry) = PublishSlotDraw(stage);

        var instructions = draw.ToSlotAssignmentInstructions(stage.Id);

        instructions.Should().ContainSingle();
        instructions[0].StageId.Should().Be(stage.Id);
        instructions[0].SlotKey.Should().Be("SF1-A");
        instructions[0].EntryId.Should().Be(entry);
    }

    [Fact]
    public void SeedMap_and_PotMembership_are_draw_inputs_not_rules()
    {
        var entry = EntryId.New();
        var seeds = new SeedMap(new Dictionary<EntryId, int> { [entry] = 3 });
        var pots = new PotMembership(new Dictionary<EntryId, int> { [entry] = 1 });
        var inputs = DrawInputs.ForSlot([entry], seeds, pots);

        inputs.SeedMap!.Seeds[entry].Should().Be(3);
        inputs.PotMembership!.Pots[entry].Should().Be(1);
    }

    [Fact]
    public void RecordResolution_rejects_entry_outside_pool()
    {
        var stage = CreateStage();
        var inPool = EntryId.New();
        var outside = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([inPool]));

        var act = () => stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(outside, "A")]),
            _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawResolutionInvalid);
    }

    [Fact]
    public void RecordResolution_rejects_when_fixed_slot_placement_missing()
    {
        var stage = CreateStage();
        var fixedEntry = EntryId.New();
        var other = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(
            draw.Id,
            DrawInputs.ForSlot(
                [fixedEntry, other],
                fixedPlacements: [new SlotDrawPlacement(fixedEntry, "A")]));

        var act = () => stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(fixedEntry, "B"),
                new SlotDrawPlacement(other, "A")
            ]),
            _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawFixedPlacementViolation);
    }

    [Fact]
    public void RecordResolution_accepts_resolution_that_includes_fixed_slot()
    {
        var stage = CreateStage();
        var fixedEntry = EntryId.New();
        var other = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(
            draw.Id,
            DrawInputs.ForSlot(
                [fixedEntry, other],
                fixedPlacements: [new SlotDrawPlacement(fixedEntry, "A")]));

        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(fixedEntry, "A"),
                new SlotDrawPlacement(other, "B")
            ]),
            _clock);

        draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
        draw.Resolution.SlotResults.Should().HaveCount(2);
    }

    [Fact]
    public void Group_draw_records_and_publishes_without_mutating_groups()
    {
        var stage = CreateStage();
        var group = stage.AddGroup("A", _clock);
        var entry = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForGroup([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedGroups([new GroupDrawPlacement(entry, group.Id)]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);

        draw.Status.Should().Be(DrawStatus.Published);
        group.EntryIds.Should().BeEmpty();
    }

    [Fact]
    public void Fixed_groups_on_slot_draw_inputs_are_rejected()
    {
        var entry = EntryId.New();
        var wrong = DrawInputs.ForGroup([entry], fixedPlacements: [new GroupDrawPlacement(entry, GroupId.New())]);

        var act = () => wrong.EnsureCompatibleWith(DrawResolutionKind.Slot);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawInputsInvalid);
    }

    [Fact]
    public void SeedMap_only_is_valid_without_PotMembership()
    {
        var a = EntryId.New();
        var b = EntryId.New();
        var seeds = new SeedMap(new Dictionary<EntryId, int> { [a] = 1, [b] = 2 });
        var inputs = DrawInputs.ForSlot([a, b], seedMap: seeds);

        inputs.SeedMap.Should().NotBeNull();
        inputs.PotMembership.Should().BeNull();
        inputs.SeedMap.Seeds[a].Should().Be(1);
    }

    [Fact]
    public void PotMembership_only_is_valid_without_SeedMap()
    {
        var a = EntryId.New();
        var b = EntryId.New();
        var pots = new PotMembership(new Dictionary<EntryId, int> { [a] = 1, [b] = 1 });
        var inputs = DrawInputs.ForSlot([a, b], potMembership: pots);

        inputs.PotMembership.Should().NotBeNull();
        inputs.SeedMap.Should().BeNull();
        inputs.PotMembership.Pots[a].Should().Be(1);
    }

    [Fact]
    public void Single_slot_resolution_remains_a_valid_draw()
    {
        var stage = CreateStage();
        stage.AddSlot("Only");
        var entry = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry, "Only")]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);

        draw.Status.Should().Be(DrawStatus.Published);
        draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
        draw.Resolution.SlotResults.Should().ContainSingle();
    }

    private Stage CreateStage() =>
        Stage.Create(_competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);

    private (Draw Draw, EntryId EntryId) PublishSlotDraw(Stage stage, string slotKey = "SF1-A")
    {
        if (stage.FindSlot(slotKey) is null)
        {
            stage.AddSlot(slotKey);
        }

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
}
