// -----------------------------------------------------------------------
// <copyright file="DrawReleaseBriefCompletionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// Brief completion scenarios for draw release: last author, reconduced slots, provenance,
/// fixed-group residue, cup coherence, and evaluator vs execute parity.
/// </summary>
public sealed class DrawReleaseBriefCompletionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 22, 14, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    private static DrawActionContext Context(Stage stage, IReadOnlyList<Match>? matches = null) =>
        DrawActionEvaluator.ForCommand(stage, matches);

    private static DrawActionsAssessment Evaluate(Stage stage, Draw draw, IReadOnlyList<Match>? matches = null) =>
        DrawActionEvaluator.Evaluate(Context(stage, matches), draw);

    [Fact]
    public void Cancel_current_draw_then_release_older_skips_reconduced_slot()
    {
        var stage = CreateSlotStage("S1", "S2");
        var e1 = EntryId.New();
        var e2 = EntryId.New();
        stage.ReplaceAffectationAuthoring([e1, e2], _clock);

        var d1 = ResolveSlotDraw(stage, [new SlotDrawPlacement(e1, "S1"), new SlotDrawPlacement(e2, "S2")]);
        stage.PublishDraw(d1.Id, _clock);
        ApplyDraw.Execute(stage, d1.Id, _clock);
        stage.CancelDraw(d1.Id, _clock);

        var d2 = ResolveSlotDraw(stage, [new SlotDrawPlacement(e1, "S1"), new SlotDrawPlacement(e2, "S2")]);
        stage.PublishDraw(d2.Id, _clock);
        ApplyDraw.Execute(stage, d2.Id, _clock);
        stage.CancelDraw(d2.Id, _clock);

        var result = ReleaseDrawAlignedPlacements.Execute(stage, d1.Id, _clock);
        result.ReleasedCount.Should().Be(0);
        result.SkippedCount.Should().Be(2);
        stage.FindSlot("S1")!.EntryId.Should().Be(e1);
        stage.FindSlot("S2")!.EntryId.Should().Be(e2);
    }

    [Fact]
    public void Published_not_applied_later_draw_blocks_release_count_on_earlier()
    {
        var stage = CreateSlotStage("S1", "S2");
        var e1 = EntryId.New();
        var e2 = EntryId.New();
        var e3 = EntryId.New();
        stage.ReplaceAffectationAuthoring([e1, e2, e3], _clock);

        var d1 = ResolveSlotDraw(stage, [new SlotDrawPlacement(e1, "S1"), new SlotDrawPlacement(e2, "S2")]);
        stage.PublishDraw(d1.Id, _clock);
        ApplyDraw.Execute(stage, d1.Id, _clock);
        stage.CancelDraw(d1.Id, _clock);

        var d2 = ResolveSlotDraw(stage, [new SlotDrawPlacement(e1, "S1"), new SlotDrawPlacement(e3, "S2")]);
        stage.PublishDraw(d2.Id, _clock);
        stage.ApplyResolvedEntry("S2", e3, _clock);

        var release = Evaluate(stage, d1).Release;
        release.ReleasableCount.Should().Be(0);
        release.State.Should().Be(StageDrawActionState.Blocked);
        release.BlockCode.Should().Be(ApplicationErrorCodes.DrawReleaseFailure);
        stage.FindSlot("S1")!.EntryId.Should().Be(e1);
        stage.FindSlot("S2")!.EntryId.Should().Be(e3);
    }

    [Fact]
    public void Fixed_group_reliquat_D1_block_D2_release_D2_does_not_remove()
    {
        var stage = CreateStage();
        var groupA = stage.AddGroup("A", _clock);
        var groupB = stage.AddGroup("B", _clock);
        var pinned = EntryId.New();
        var drawn = EntryId.New();
        stage.ReplaceAffectationAuthoring([pinned, drawn], _clock);
        stage.AssignEntryToGroup(groupA.Id, pinned);

        var d1 = ResolveGroupDraw(stage, drawn, groupB.Id);
        stage.PublishDraw(d1.Id, _clock);
        ApplyDraw.Execute(stage, d1.Id, _clock);
        stage.CancelDraw(d1.Id, _clock);

        var d2 = ResolveGroupDraw(stage, drawn, groupB.Id);
        stage.PublishDraw(d2.Id, _clock);
        ApplyDraw.Execute(stage, d2.Id, _clock);

        Evaluate(stage, d1).Release.ReleasableCount.Should().Be(0);
        Evaluate(stage, d1).Release.State.Should().Be(StageDrawActionState.Blocked);
        var act = () => ReleaseDrawAlignedPlacements.Execute(stage, d1.Id, _clock);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawReleaseFailure);

        stage.CancelDraw(d2.Id, _clock);
        var result = ReleaseDrawAlignedPlacements.Execute(stage, d2.Id, _clock);
        result.ReleasedCount.Should().Be(1);
        result.SkippedCount.Should().Be(0);
        groupA.EntryIds.Should().ContainSingle(id => id.Equals(pinned));
        groupB.EntryIds.Should().BeEmpty();
    }

    [Fact]
    public void Qualification_on_slot_makes_release_count_zero_and_execute_skips()
    {
        var stage = CreateSlotStage("S1");
        var entry = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry], _clock);
        var draw = ResolveSlotDraw(stage, [new SlotDrawPlacement(entry, "S1")]);
        stage.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.CancelDraw(draw.Id, _clock);
        stage.WriteSlotResolutionProvenance(
            "S1",
            new ResolutionProvenance(
                ResolutionProvenanceKind.Qualification,
                "Q|path",
                "fp"));

        Evaluate(stage, draw).Release.ReleasableCount.Should().Be(0);
        var result = ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);
        result.ReleasedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        stage.FindSlot("S1")!.EntryId.Should().Be(entry);
    }

    [Fact]
    public void Group_qualification_provenance_skips_release()
    {
        var stage = CreateStage();
        var group = stage.AddGroup("A", _clock);
        var entry = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry], _clock);
        var draw = ResolveGroupDraw(stage, entry, group.Id);
        stage.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.CancelDraw(draw.Id, _clock);
        stage.WriteGroupResolutionProvenance(
            group.Id,
            entry,
            new ResolutionProvenance(
                ResolutionProvenanceKind.Qualification,
                "Q|group-path",
                "fp"));

        Evaluate(stage, draw).Release.ReleasableCount.Should().Be(0);
        var result = ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);
        result.ReleasedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        group.EntryIds.Should().ContainSingle(id => id.Equals(entry));
    }

    [Fact]
    public void Manual_group_replacement_after_release_accepted_residue()
    {
        var stage = CreateStage();
        var group = stage.AddGroup("A", _clock);
        var other = stage.AddGroup("B", _clock);
        var entry = EntryId.New();
        var replacement = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry, replacement], _clock);
        var draw = ResolveGroupDraw(stage, entry, group.Id);
        stage.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.CancelDraw(draw.Id, _clock);

        ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock).ReleasedCount.Should().Be(1);
        group.EntryIds.Should().BeEmpty();

        stage.AssignEntryToGroup(group.Id, replacement);

        Evaluate(stage, draw).Release.ReleasableCount.Should().Be(0);
        var second = ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);
        second.ReleasedCount.Should().Be(0);
        second.SkippedCount.Should().Be(1);
        group.EntryIds.Should().ContainSingle(id => id.Equals(replacement));
        other.EntryIds.Should().BeEmpty();
    }

    [Fact]
    public void Published_cancelled_without_apply_leaves_count_zero()
    {
        var stage = CreateSlotStage("S1");
        var entry = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry], _clock);
        var draw = ResolveSlotDraw(stage, [new SlotDrawPlacement(entry, "S1")]);
        stage.PublishDraw(draw.Id, _clock);
        stage.CancelDraw(draw.Id, _clock);

        var release = Evaluate(stage, draw).Release;
        release.ReleasableCount.Should().Be(0);
        release.State.Should().Be(StageDrawActionState.Hidden);

        var result = ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);
        result.ReleasedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        stage.FindSlot("S1")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Draws_list_order_determines_last_author()
    {
        var stage = CreateSlotStage("S1");
        var firstEntry = EntryId.New();
        var secondEntry = EntryId.New();
        stage.ReplaceAffectationAuthoring([firstEntry, secondEntry], _clock);

        var d1 = ResolveSlotDraw(stage, [new SlotDrawPlacement(firstEntry, "S1")]);
        stage.PublishDraw(d1.Id, _clock);
        ApplyDraw.Execute(stage, d1.Id, _clock);
        stage.CancelDraw(d1.Id, _clock);

        var d2 = ResolveSlotDraw(stage, [new SlotDrawPlacement(secondEntry, "S1")]);
        stage.PublishDraw(d2.Id, _clock);
        stage.ApplyResolvedEntry("S1", secondEntry, _clock);
        stage.CancelDraw(d2.Id, _clock);

        var index = DrawReleaseReleasability.BuildIndex(stage);
        index.SlotLastAuthorByKey["S1"].Should().Be(d2.Id);
        Evaluate(stage, d1).Release.ReleasableCount.Should().Be(0);
        Evaluate(stage, d2).Release.ReleasableCount.Should().Be(1);
    }

    [Fact]
    public void Parity_releasable_matches_execute_on_a_multi_slot_case()
    {
        var stage = CreateSlotStage("S1", "S2", "S3");
        var e1 = EntryId.New();
        var e2 = EntryId.New();
        var e3 = EntryId.New();
        stage.ReplaceAffectationAuthoring([e1, e2, e3], _clock);

        var draw = ResolveSlotDraw(
            stage,
            [
                new SlotDrawPlacement(e1, "S1"),
                new SlotDrawPlacement(e2, "S2"),
                new SlotDrawPlacement(e3, "S3")
            ]);
        stage.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.CancelDraw(draw.Id, _clock);
        stage.AssignEntryToSlot("S1", e1);

        var count = Evaluate(stage, draw).Release.ReleasableCount;
        count.Should().Be(2);
        var result = ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);
        result.ReleasedCount.Should().Be(count);
        result.SkippedCount.Should().Be(1);
        stage.FindSlot("S1")!.EntryId.Should().Be(e1);
        stage.FindSlot("S2")!.EntryId.Should().BeNull();
        stage.FindSlot("S3")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Cup_coherence_block_when_match_on_reconduced_slot()
    {
        var stage = CreateSlotStage("S1", "S2");
        stage.SeedEntryRoundBracketPairs();
        var round = stage.AddRound("Tour", _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        stage.ReplaceAffectationAuthoring([home, away], _clock);

        var d1 = ResolveSlotDraw(
            stage,
            [new SlotDrawPlacement(home, "S1"), new SlotDrawPlacement(away, "S2")]);
        stage.PublishDraw(d1.Id, _clock);
        ApplyDraw.Execute(stage, d1.Id, _clock);
        stage.CancelDraw(d1.Id, _clock);

        var d2 = ResolveSlotDraw(stage, [new SlotDrawPlacement(home, "S1")]);
        stage.PublishDraw(d2.Id, _clock);
        ApplyDraw.Execute(stage, d2.Id, _clock);
        stage.CancelDraw(d2.Id, _clock);

        var fixture = stage.AddFixture(round.Id, _clock, "S1", "S2", "P1");
        var match = Match.Create(_competitionId, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);

        var release = Evaluate(stage, d1, [match]).Release;
        release.ReleasableCount.Should().BeGreaterThan(0);
        release.State.Should().Be(StageDrawActionState.Blocked);
        release.BlockCode.Should().Be(StageErrorCodes.MaterializedOpponentConflict);

        var act = () => ReleaseDrawAlignedPlacements.Execute(stage, d1.Id, _clock, [match]);
        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MaterializedOpponentConflict);
        stage.FindSlot("S2")!.EntryId.Should().Be(away);
    }

    private Stage CreateStage() =>
        Stage.Create(_competitionId, new StageName("Phase"), SampleRegulations.Standard(), _clock);

    private Stage CreateSlotStage(params string[] keys)
    {
        var stage = CreateStage();
        foreach (var key in keys)
        {
            stage.AddSlot(key);
        }

        return stage;
    }

    private Draw ResolveSlotDraw(Stage stage, IReadOnlyList<SlotDrawPlacement> placements)
    {
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        var entries = placements.Select(placement => placement.EntryId).Distinct().ToArray();
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot(entries));
        stage.RecordDrawResolution(draw.Id, DrawResolution.ResolvedSlots(placements), _clock);
        return draw;
    }

    private Draw ResolveGroupDraw(Stage stage, EntryId entry, GroupId groupId)
    {
        var draw = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForGroup([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedGroups([new GroupDrawPlacement(entry, groupId)]),
            _clock);
        return draw;
    }
}
