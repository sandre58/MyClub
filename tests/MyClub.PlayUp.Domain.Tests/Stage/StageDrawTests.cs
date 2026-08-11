// -----------------------------------------------------------------------
// <copyright file="StageDrawTests.cs" company="Stéphane ANDRE">
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
    public void ConfigureDrawInputs_is_sole_entry_point_for_pool()
    {
        var stage = CreateStage();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        var entries = new[] { EntryId.New(), EntryId.New() };

        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot(entries), _clock);

        draw.Inputs!.Entries.Should().Equal(entries);
    }

    [Fact]
    public void RecordResolution_kind_mismatch_is_rejected()
    {
        var stage = CreateStage();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        var entry = EntryId.New();
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]), _clock);
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
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([EntryId.New()]), _clock);
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
            DrawInputs.ForSlot([EntryId.New()]),
            _clock);
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
    public void Fixed_slot_placements_do_not_create_DirectAssignment()
    {
        var stage = CreateStage();
        stage.AddSlot("A", _clock);
        var entry = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(
            draw.Id,
            DrawInputs.ForSlot([entry], fixedPlacements: [new SlotDrawPlacement(entry, "A")]),
            _clock);

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

    private StageAggregate CreateStage() =>
        StageAggregate.Create(_competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);

    private (Draw Draw, EntryId EntryId) PublishSlotDraw(StageAggregate stage, string slotKey = "SF1-A")
    {
        if (stage.FindSlot(slotKey) is null)
        {
            stage.AddSlot(slotKey, _clock);
        }

        var entry = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]), _clock);
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry, slotKey)]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        return (draw, entry);
    }
}
