// -----------------------------------------------------------------------
// <copyright file="ReleaseDrawAlignedPlacementsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class ReleaseDrawAlignedPlacementsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 23, 22, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void Execute_aligned_occupant_is_cleared()
    {
        var stage = CreateStage();
        var (draw, entry) = PublishAndApplySlot(stage, "S1");
        stage.CancelDraw(draw.Id, _clock);
        stage.ClearDomainEvents();

        var result = ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);

        result.ReleasedCount.Should().Be(1);
        result.SkippedCount.Should().Be(0);
        stage.FindSlot("S1")!.EntryId.Should().BeNull();
        _ = entry;
    }

    [Fact]
    public void Execute_divergent_dynamic_occupant_is_skipped()
    {
        var stage = CreateStage();
        var (draw, entryA) = PublishAndApplySlot(stage, "S1");
        stage.CancelDraw(draw.Id, _clock);

        var entryB = EntryId.New();
        stage.ReplaceCompositionEntries([entryA, entryB], _clock);
        stage.ApplyResolvedEntry("S1", entryB, _clock);
        stage.ClearDomainEvents();

        var result = ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);

        result.ReleasedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        stage.FindSlot("S1")!.EntryId.Should().Be(entryB);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Execute_direct_assignment_divergent_is_skipped_and_intact()
    {
        var stage = CreateStage();
        var (draw, entryA) = PublishAndApplySlot(stage, "S1");
        stage.CancelDraw(draw.Id, _clock);

        var entryB = EntryId.New();
        stage.ReplaceCompositionEntries([entryA, entryB], _clock);
        stage.AssignEntryToSlot("S1", entryB);
        var directs = stage.DirectAssignments.ToArray();
        stage.ClearDomainEvents();

        var result = ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);

        result.ReleasedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        stage.FindSlot("S1")!.EntryId.Should().Be(entryB);
        stage.DirectAssignments.Should().Equal(directs);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Execute_rejects_group_draw()
    {
        var stage = CreateStage();
        var group = stage.AddGroup("A", _clock);
        var entry = EntryId.New();
        stage.ReplaceCompositionEntries([entry], _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForGroup([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedGroups([new GroupDrawPlacement(entry, group.Id)]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        stage.ClearDomainEvents();

        var act = () => ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawKindNotSupported);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Execute_rejects_not_resolved()
    {
        var stage = CreateStage();
        stage.AddSlot("S1");
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([EntryId.New()]));
        stage.ClearDomainEvents();

        var act = () => ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawReleaseFailure);
        stage.DomainEvents.Should().BeEmpty();
    }

    private Stage CreateStage() =>
        Stage.Create(_competitionId, new StageName("Phase"), SampleRegulations.Standard(), _clock);

    private (Draw Draw, EntryId Entry) PublishAndApplySlot(Stage stage, string slotKey)
    {
        stage.AddSlot(slotKey);
        var entry = EntryId.New();
        stage.ReplaceCompositionEntries([entry], _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry, slotKey)]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        return (draw, entry);
    }
}
