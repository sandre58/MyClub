// -----------------------------------------------------------------------
// <copyright file="ReleaseDrawAlignedPlacementsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
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
        stage.ReplaceAffectationAuthoring([entryA, entryB], _clock);
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
        stage.ReplaceAffectationAuthoring([entryA, entryB], _clock);
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
    public void Execute_group_aligned_membership_is_cleared()
    {
        var stage = CreateStage();
        var group = stage.AddGroup("A", _clock);
        var entry = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry], _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForGroup([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedGroups([new GroupDrawPlacement(entry, group.Id)]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.CancelDraw(draw.Id, _clock);
        stage.ClearDomainEvents();

        var result = ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);

        result.ReleasedCount.Should().Be(1);
        result.SkippedCount.Should().Be(0);
        stage.FindGroup(group.Id)!.EntryIds.Should().BeEmpty();
    }

    [Fact]
    public void Execute_rejects_when_another_published_draw_is_applied_on_stage()
    {
        var stage = CreateStage();
        var group = stage.AddGroup("A", _clock);
        var entry = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry], _clock);
        var cancelled = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(cancelled.Id, DrawInputs.ForGroup([entry]));
        stage.RecordDrawResolution(
            cancelled.Id,
            DrawResolution.ResolvedGroups([new GroupDrawPlacement(entry, group.Id)]),
            _clock);
        stage.PublishDraw(cancelled.Id, _clock);
        ApplyDraw.Execute(stage, cancelled.Id, _clock);
        stage.CancelDraw(cancelled.Id, _clock);

        var active = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(active.Id, DrawInputs.ForGroup([entry]));
        stage.RecordDrawResolution(
            active.Id,
            DrawResolution.ResolvedGroups([new GroupDrawPlacement(entry, group.Id)]),
            _clock);
        stage.PublishDraw(active.Id, _clock);
        ApplyDraw.Execute(stage, active.Id, _clock);
        stage.ClearDomainEvents();

        var act = () => ReleaseDrawAlignedPlacements.Execute(stage, cancelled.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawReleaseFailure);
        stage.FindGroup(group.Id)!.EntryIds.Should().ContainSingle(id => id.Equals(entry));
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
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawReleaseRequiresCancelled);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Execute_rejects_cancelled_draw_that_is_not_resolved()
    {
        var stage = CreateStage();
        stage.AddSlot("S1");
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([EntryId.New()]));
        stage.CancelDraw(draw.Id, _clock);
        stage.ClearDomainEvents();

        var act = () => ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawReleaseFailure);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Execute_slot_Running_is_refused()
    {
        var stage = CreateStage();
        stage.AddRound("Tour", _clock);
        var (draw, _) = PublishAndApplySlot(stage, "S1");
        stage.CancelDraw(draw.Id, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        stage.FindSlot("S1")!.EntryId.Should().NotBeNull();
        stage.Status.Should().Be(StageStatus.Running);
    }

    [Fact]
    public void Execute_slot_Suspended_is_refused()
    {
        var stage = CreateStage();
        stage.AddRound("Tour", _clock);
        var (draw, _) = PublishAndApplySlot(stage, "S1");
        stage.CancelDraw(draw.Id, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.Suspend(_clock);

        var act = () => ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        stage.FindSlot("S1")!.EntryId.Should().NotBeNull();
        stage.Status.Should().Be(StageStatus.Suspended);
    }

    [Fact]
    public void Execute_group_aligned_membership_in_Ready_is_cleared_and_stage_stays_Ready()
    {
        var stage = CreateStage();
        var (draw, group, _) = PublishApplyCancelGroup(stage, withMatchday: true);
        stage.Prepare(_clock);

        var result = ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);

        result.ReleasedCount.Should().Be(1);
        result.SkippedCount.Should().Be(0);
        group.EntryIds.Should().BeEmpty();
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void Execute_group_Ready_with_attached_matches_is_refused()
    {
        var stage = CreateStage();
        var (draw, group, entry) = PublishApplyCancelGroup(stage, withMatchday: true);
        var home = EntryId.New();
        var away = EntryId.New();
        stage.AssignEntryToGroup(group.Id, home);
        stage.AssignEntryToGroup(group.Id, away);
        var fixture = stage.AddFixture(stage.Matchdays[0].Id, _clock);
        var match = Match.Create(_competitionId, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        stage.Prepare(_clock);

        var act = () => ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock, [match]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawReleaseBlockedByAttachedMatches);
        group.EntryIds.Should().Contain(entry);
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void Execute_group_Draft_entry_on_attached_match_keeps_opponent_coherence()
    {
        var stage = CreateStage();
        var matchday = stage.AddMatchday(1, _clock);
        var (draw, group, entry) = PublishApplyCancelGroup(stage);
        var other = EntryId.New();
        stage.AssignEntryToGroup(group.Id, other);
        var fixture = stage.AddFixture(matchday.Id, _clock);
        var match = Match.Create(_competitionId, stage.Id, entry, other, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);

        var act = () => ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock, [match]);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MaterializedOpponentConflict);
        group.EntryIds.Should().Contain(entry);
        stage.Status.Should().Be(StageStatus.Draft);
    }

    [Fact]
    public void Execute_group_Running_is_refused()
    {
        var stage = CreateStage();
        var (draw, group, entry) = PublishApplyCancelGroup(stage, withMatchday: true);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        group.EntryIds.Should().Contain(entry);
        stage.Status.Should().Be(StageStatus.Running);
    }

    [Fact]
    public void Execute_slot_aligned_occupant_in_Ready_is_cleared_and_stage_stays_Ready()
    {
        var stage = CreateStage();
        stage.AddRound("Tour", _clock);
        var (draw, _) = PublishAndApplySlot(stage, "S1");
        stage.CancelDraw(draw.Id, _clock);
        stage.Prepare(_clock);

        var result = ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);

        result.ReleasedCount.Should().Be(1);
        result.SkippedCount.Should().Be(0);
        stage.FindSlot("S1")!.EntryId.Should().BeNull();
        stage.Status.Should().Be(StageStatus.Ready);
    }

    private Stage CreateStage() =>
        Stage.Create(_competitionId, new StageName("Phase"), SampleRegulations.Standard(), _clock);

    private (Draw Draw, EntryId Entry) PublishAndApplySlot(Stage stage, string slotKey)
    {
        stage.AddSlot(slotKey);
        var entry = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry], _clock);
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

    private (Draw Draw, Group Group, EntryId Entry) PublishApplyCancelGroup(Stage stage, bool withMatchday = false)
    {
        if (withMatchday)
        {
            stage.AddMatchday(1, _clock);
        }

        var group = stage.AddGroup("A", _clock);
        var entry = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry], _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForGroup([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedGroups([new GroupDrawPlacement(entry, group.Id)]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.CancelDraw(draw.Id, _clock);
        return (draw, group, entry);
    }
}
