// -----------------------------------------------------------------------
// <copyright file="DrawActionEvaluatorParityTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// Evaluator block codes match the use case that would run. Available is success for the preflight
/// the evaluator owns; placement-level apply failures stay outside that biconditional.
/// </summary>
public sealed class DrawActionEvaluatorParityTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void Release_available_matches_released_count_and_skips_direct_assignment()
    {
        var stage = CreateStage();
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        var kept = EntryId.New();
        var released = EntryId.New();
        stage.ReplaceAffectationAuthoring([kept, released], _clock);
        var draw = ResolveSlotDraw(stage, [new SlotDrawPlacement(kept, "S1"), new SlotDrawPlacement(released, "S2")]);
        stage.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.CancelDraw(draw.Id, _clock);
        stage.AssignEntryToSlot("S1", kept);

        var release = Evaluate(stage, draw).Release;
        release.State.Should().Be(StageDrawActionState.Available);
        release.ReleasableCount.Should().Be(1);

        var result = ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);
        result.ReleasedCount.Should().Be(release.ReleasableCount);
        stage.FindSlot("S1")!.EntryId.Should().Be(kept);
        stage.FindSlot("S2")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Release_cup_attached_pair_matches_materialized_opponent_conflict()
    {
        var stage = CreateStage();
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        stage.SeedEntryRoundBracketPairs();
        var round = stage.AddRound("Tour", _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        stage.ReplaceAffectationAuthoring([home, away], _clock);
        var draw = ResolveSlotDraw(stage, [new SlotDrawPlacement(home, "S1"), new SlotDrawPlacement(away, "S2")]);
        stage.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        var fixture = stage.AddFixture(round.Id, _clock, "S1", "S2", "P1");
        var match = Match.Create(_competitionId, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);

        var redo = Evaluate(stage, draw, [match]).Redo;
        redo.State.Should().Be(StageDrawActionState.Blocked);
        redo.BlockCode.Should().Be(StageErrorCodes.MaterializedOpponentConflict);
        CancelDraw.Execute(stage, draw.Id, _clock);
        AssertBlocked(
            () => ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock, [match]),
            StageErrorCodes.MaterializedOpponentConflict);
        stage.FindSlot("S1")!.EntryId.Should().Be(home);
    }

    [Fact]
    public void Release_group_ready_with_matches_matches_attached_block()
    {
        var (stage, draw, match) = GroupWithAttachedMatch(StageStatus.Ready);
        AssertBlocked(
            () => ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock, [match]),
            ApplicationErrorCodes.DrawReleaseBlockedByAttachedMatches);
        Evaluate(stage, draw, [match]).Release.BlockCode
            .Should().Be(ApplicationErrorCodes.DrawReleaseBlockedByAttachedMatches);
    }

    [Fact]
    public void Release_group_draft_with_matches_matches_opponent_conflict()
    {
        var (stage, draw, match) = GroupWithAttachedMatch(StageStatus.Draft);
        AssertBlocked(
            () => ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock, [match]),
            StageErrorCodes.MaterializedOpponentConflict);
        Evaluate(stage, draw, [match]).Release.State.Should().Be(StageDrawActionState.Blocked);
    }

    [Fact]
    public void Release_blocked_when_another_published_draw_is_applied()
    {
        var stage = CreateStage();
        var group = stage.AddGroup("A", _clock);
        var entry = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry], _clock);
        var cancelled = ResolveGroupDraw(stage, entry, group.Id);
        stage.PublishDraw(cancelled.Id, _clock);
        ApplyDraw.Execute(stage, cancelled.Id, _clock);
        stage.CancelDraw(cancelled.Id, _clock);
        var active = ResolveGroupDraw(stage, entry, group.Id);
        stage.PublishDraw(active.Id, _clock);
        ApplyDraw.Execute(stage, active.Id, _clock);

        Evaluate(stage, cancelled).Release.BlockCode.Should().Be(ApplicationErrorCodes.DrawReleaseFailure);
        AssertBlocked(
            () => ReleaseDrawAlignedPlacements.Execute(stage, cancelled.Id, _clock),
            ApplicationErrorCodes.DrawReleaseFailure);
    }

    [Fact]
    public void Apply_obsolete_after_pool_shrink_matches_command()
    {
        var stage = CreateStage();
        var group = stage.AddGroup("A", _clock);
        var kept = EntryId.New();
        var dropped = EntryId.New();
        stage.ReplaceAffectationAuthoring([kept, dropped], _clock);
        var draw = ResolveGroupDraw(stage, dropped, group.Id);
        stage.PublishDraw(draw.Id, _clock);
        stage.ReplaceAffectationAuthoring([kept], _clock);

        DrawConsumptionConformance.IsObsoleteForConsumption(draw, stage).Should().BeTrue();
        Evaluate(stage, draw).Apply.BlockCode.Should().Be(ApplicationErrorCodes.DrawObsoleteForConsumption);
        AssertBlocked(
            () => ApplyDraw.Execute(stage, draw.Id, _clock),
            ApplicationErrorCodes.DrawObsoleteForConsumption);
        group.EntryIds.Should().BeEmpty();
    }

    [Fact]
    public void Apply_available_places_the_resolved_entry()
    {
        var stage = CreateStage();
        stage.AddSlot("S1");
        var entry = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry], _clock);
        var draw = ResolveSlotDraw(stage, [new SlotDrawPlacement(entry, "S1")]);
        stage.PublishDraw(draw.Id, _clock);

        Evaluate(stage, draw).Apply.State.Should().Be(StageDrawActionState.Available);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.FindSlot("S1")!.EntryId.Should().Be(entry);
    }

    [Fact]
    public void Release_running_and_suspended_slot_match_structure_locked()
    {
        foreach (var suspend in new[] { false, true })
        {
            var stage = CreateStage();
            stage.AddRound("Tour", _clock);
            stage.AddSlot("S1");
            var entry = EntryId.New();
            stage.ReplaceAffectationAuthoring([entry], _clock);
            var draw = ResolveSlotDraw(stage, [new SlotDrawPlacement(entry, "S1")]);
            stage.PublishDraw(draw.Id, _clock);
            ApplyDraw.Execute(stage, draw.Id, _clock);
            stage.CancelDraw(draw.Id, _clock);
            stage.Prepare(_clock);
            stage.Start(_clock);
            if (suspend)
            {
                stage.Suspend(_clock);
            }

            Evaluate(stage, draw).Release.BlockCode.Should().Be(StageErrorCodes.StructureLocked);
            AssertBlocked(
                () => ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock),
                StageErrorCodes.StructureLocked);
        }
    }

    [Fact]
    public void Release_non_cancelled_matches_requires_cancelled()
    {
        var stage = CreateStage();
        stage.AddSlot("S1");
        var entry = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry], _clock);
        var draw = ResolveSlotDraw(stage, [new SlotDrawPlacement(entry, "S1")]);
        stage.PublishDraw(draw.Id, _clock);

        Evaluate(stage, draw).Release.BlockCode.Should().Be(ApplicationErrorCodes.DrawReleaseRequiresCancelled);
        AssertBlocked(
            () => ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock),
            ApplicationErrorCodes.DrawReleaseRequiresCancelled);
    }

    [Fact]
    public void Overview_exposes_actions_and_newest_release_candidate()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("Phase"), SampleRegulations.Standard(), _clock);
        var olderEntry = EntryId.New();
        var newerEntry = EntryId.New();
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        stage.ReplaceAffectationAuthoring([olderEntry, newerEntry], _clock);
        var older = ResolveSlotDraw(stage, [new SlotDrawPlacement(olderEntry, "S1")]);
        stage.PublishDraw(older.Id, _clock);
        ApplyDraw.Execute(stage, older.Id, _clock);
        stage.CancelDraw(older.Id, _clock);
        stage.ClearResolvedEntry("S1", _clock);
        var newer = ResolveSlotDraw(stage, [new SlotDrawPlacement(newerEntry, "S2")]);
        stage.PublishDraw(newer.Id, _clock);
        ApplyDraw.Execute(stage, newer.Id, _clock);
        stage.CancelDraw(newer.Id, _clock);

        var overview = StageOverviewAssembler.Assemble(stage, competition);
        overview.ReleaseCandidateDrawId.Should().Be(newer.Id.Value);
        var newerDraw = overview.Draws.Single(draw => draw.Id == newer.Id.Value);
        newerDraw.Actions.Should().NotBeNull();
        newerDraw.Actions!.Release.State.Should().Be(StageDrawActionState.Available);
        newerDraw.Actions.Release.ReleasableCount.Should().Be(1);
        newerDraw.Actions.Cancel.State.Should().Be(StageDrawActionState.Hidden);
    }

    private static DrawActionsAssessment Evaluate(Stage stage, Draw draw, IReadOnlyList<Match>? matches = null) =>
        DrawActionEvaluator.Evaluate(DrawActionEvaluator.ForCommand(stage, matches), draw);

    private static void AssertBlocked(Action act, string code)
    {
        var exception = act.Should().Throw<Exception>().Which;
        var actual = exception switch
        {
            ApplicationFailureException application => application.Code,
            DomainException domain => domain.Code,
            _ => null
        };
        actual.Should().Be(code);
    }

    private Stage CreateStage() =>
        Stage.Create(_competitionId, new StageName("Phase"), SampleRegulations.Standard(), _clock);

    private Draw ResolveSlotDraw(Stage stage, IReadOnlyList<SlotDrawPlacement> placements)
    {
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([.. placements.Select(placement => placement.EntryId)]));
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

    private (Stage Stage, Draw Draw, Match Match) GroupWithAttachedMatch(StageStatus status)
    {
        var stage = CreateStage();
        var matchday = stage.AddMatchday(1, _clock);
        var group = stage.AddGroup("A", _clock);
        var entry = EntryId.New();
        var other = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry, other], _clock);
        var draw = ResolveGroupDraw(stage, entry, group.Id);
        stage.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.CancelDraw(draw.Id, _clock);
        stage.AssignEntryToGroup(group.Id, other);
        var fixture = stage.AddFixture(matchday.Id, _clock);
        var match = Match.Create(_competitionId, stage.Id, entry, other, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        if (status == StageStatus.Ready)
        {
            stage.Prepare(_clock);
        }

        return (stage, draw, match);
    }
}
