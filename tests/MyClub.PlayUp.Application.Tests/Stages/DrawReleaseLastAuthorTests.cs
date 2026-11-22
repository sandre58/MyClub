// -----------------------------------------------------------------------
// <copyright file="DrawReleaseLastAuthorTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class DrawReleaseLastAuthorTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 22, 10, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    private static DrawActionContext Context(Stage stage) =>
        DrawActionEvaluator.ForCommand(stage, null);

    private static DrawActionsAssessment Evaluate(Stage stage, Draw draw) =>
        DrawActionEvaluator.Evaluate(Context(stage), draw);

    [Theory]
    [InlineData(DrawResolutionKind.Slot)]
    [InlineData(DrawResolutionKind.Group)]
    public void Cancelled_draw_does_not_count_placements_rewritten_by_a_later_applied_draw(
        DrawResolutionKind kind)
    {
        var stage = CreateStage(kind);
        var first = CreatePublishApplyCancel(kind, stage, "S1", "S2", "S3");
        ReleaseDrawAlignedPlacements.Execute(stage, first.Id, _clock);
        var second = CreatePublishApply(kind, stage, "S1", "S2", "S3", sameAs: first);

        var release = Evaluate(stage, first).Release;
        release.ReleasableCount.Should().Be(0);
        release.State.Should().Be(StageDrawActionState.Blocked);
        release.BlockCode.Should().Be(ApplicationErrorCodes.DrawReleaseFailure);
        DrawActionEvaluator.ReleaseCandidateDrawId(Context(stage), stage).Should().BeNull();

        var act = () => ReleaseDrawAlignedPlacements.Execute(stage, first.Id, _clock);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawReleaseFailure);
    }

    [Fact]
    public void Release_skips_slot_reconduced_by_later_draw_without_clearing()
    {
        var stage = CreateStage(DrawResolutionKind.Slot);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
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
    }

    [Fact]
    public void Published_not_applied_later_draw_still_claims_reconduced_slot()
    {
        var stage = CreateStage(DrawResolutionKind.Slot);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
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

        Evaluate(stage, d1).Release.ReleasableCount.Should().Be(0);
        stage.FindSlot("S1")!.EntryId.Should().Be(e1);
        stage.FindSlot("S2")!.EntryId.Should().Be(e3);
    }

    [Fact]
    public void Release_candidate_null_when_newest_blocked_and_older_available()
    {
        var stage = CreateStage(DrawResolutionKind.Group);
        stage.AddMatchday(1, _clock);
        var groupA = stage.Groups[0];
        var groupB = stage.AddGroup("B", _clock);
        var e1 = EntryId.New();
        var e2 = EntryId.New();
        var other = EntryId.New();
        stage.ReplaceAffectationAuthoring([e1, e2, other], _clock);

        var d1 = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(d1.Id, DrawInputs.ForGroup([e1]));
        stage.RecordDrawResolution(
            d1.Id,
            DrawResolution.ResolvedGroups([new GroupDrawPlacement(e1, groupA.Id)]),
            _clock);
        stage.PublishDraw(d1.Id, _clock);
        ApplyDraw.Execute(stage, d1.Id, _clock);
        stage.CancelDraw(d1.Id, _clock);

        var d2 = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(d2.Id, DrawInputs.ForGroup([e2]));
        stage.RecordDrawResolution(
            d2.Id,
            DrawResolution.ResolvedGroups([new GroupDrawPlacement(e2, groupB.Id)]),
            _clock);
        stage.PublishDraw(d2.Id, _clock);
        ApplyDraw.Execute(stage, d2.Id, _clock);
        stage.CancelDraw(d2.Id, _clock);

        stage.AssignEntryToGroup(groupB.Id, other);
        var fixture = stage.AddFixture(stage.Matchdays[0].Id, _clock);
        var match = MyClub.PlayUp.Domain.Matches.Match.Create(_competitionId, stage.Id, e2, other, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);

        var context = DrawActionEvaluator.ForCommand(stage, [match]);
        DrawActionEvaluator.Evaluate(context, d1).Release.ReleasableCount.Should().Be(1);
        DrawActionEvaluator.Evaluate(context, d1).Release.State.Should().Be(StageDrawActionState.Available);
        DrawActionEvaluator.Evaluate(context, d2).Release.ReleasableCount.Should().Be(1);
        DrawActionEvaluator.Evaluate(context, d2).Release.State.Should().Be(StageDrawActionState.Blocked);
        DrawActionEvaluator.Evaluate(context, d2).Release.BlockCode.Should()
            .Be(StageErrorCodes.MaterializedOpponentConflict);
        DrawActionEvaluator.ReleaseCandidateDrawId(context, stage).Should().BeNull();
    }

    [Fact]
    public void Evaluate_release_count_matches_execute_released_count_for_slot()
    {
        var stage = CreateStage(DrawResolutionKind.Slot);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        var kept = EntryId.New();
        var released = EntryId.New();
        stage.ReplaceAffectationAuthoring([kept, released], _clock);
        var draw = ResolveSlotDraw(
            stage,
            [new SlotDrawPlacement(kept, "S1"), new SlotDrawPlacement(released, "S2")]);
        stage.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.CancelDraw(draw.Id, _clock);
        stage.AssignEntryToSlot("S1", kept);

        var count = Evaluate(stage, draw).Release.ReleasableCount;
        var result = ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);
        result.ReleasedCount.Should().Be(count);
    }

    private Stage CreateStage(DrawResolutionKind kind)
    {
        var stage = Stage.Create(_competitionId, new StageName("Phase"), SampleRegulations.Standard(), _clock);
        if (kind == DrawResolutionKind.Group)
        {
            stage.AddGroup("A", _clock);
        }

        return stage;
    }

    private Draw CreatePublishApplyCancel(
        DrawResolutionKind kind,
        Stage stage,
        string s1,
        string s2,
        string s3)
    {
        var draw = CreatePublishApply(kind, stage, s1, s2, s3, sameAs: null);
        stage.CancelDraw(draw.Id, _clock);
        return draw;
    }

    private Draw CreatePublishApply(
        DrawResolutionKind kind,
        Stage stage,
        string s1,
        string s2,
        string s3,
        Draw? sameAs)
    {
        if (kind == DrawResolutionKind.Slot)
        {
            if (sameAs is null)
            {
                stage.AddSlot(s1);
                stage.AddSlot(s2);
                stage.AddSlot(s3);
            }

            var entries = sameAs?.Resolution.SlotResults.Select(p => p.EntryId).ToArray()
                ?? [EntryId.New(), EntryId.New(), EntryId.New()];
            if (sameAs is null)
            {
                stage.ReplaceAffectationAuthoring(entries, _clock);
            }

            var placements = new[]
            {
                new SlotDrawPlacement(entries[0], s1),
                new SlotDrawPlacement(entries[1], s2),
                new SlotDrawPlacement(entries[2], s3),
            };
            var draw = ResolveSlotDraw(stage, placements);
            stage.PublishDraw(draw.Id, _clock);
            ApplyDraw.Execute(stage, draw.Id, _clock);
            return draw;
        }

        var group = stage.Groups[0];
        var other = stage.AddGroup("B", _clock);
        var third = stage.AddGroup("C", _clock);
        EntryId entry;
        EntryId e2;
        EntryId e3;
        if (sameAs is null)
        {
            entry = EntryId.New();
            e2 = EntryId.New();
            e3 = EntryId.New();
            stage.ReplaceAffectationAuthoring([entry, e2, e3], _clock);
        }
        else
        {
            var prior = sameAs.Resolution.GroupResults;
            entry = prior[0].EntryId;
            e2 = prior[1].EntryId;
            e3 = prior[2].EntryId;
            stage.ReplaceAffectationAuthoring([entry, e2, e3], _clock);
        }

        var drawG = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(drawG.Id, DrawInputs.ForGroup([entry, e2, e3]));
        stage.RecordDrawResolution(
            drawG.Id,
            DrawResolution.ResolvedGroups(
            [
                new GroupDrawPlacement(entry, group.Id),
                new GroupDrawPlacement(e2, other.Id),
                new GroupDrawPlacement(e3, third.Id)
            ]),
            _clock);
        stage.PublishDraw(drawG.Id, _clock);
        ApplyDraw.Execute(stage, drawG.Id, _clock);
        return drawG;
    }

    private Draw ResolveSlotDraw(Stage stage, IReadOnlyList<SlotDrawPlacement> placements)
    {
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        var entries = placements.Select(p => p.EntryId).Distinct().ToArray();
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot(entries));
        stage.RecordDrawResolution(draw.Id, DrawResolution.ResolvedSlots(placements), _clock);
        return draw;
    }
}
