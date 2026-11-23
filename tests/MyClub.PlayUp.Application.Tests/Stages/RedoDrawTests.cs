// -----------------------------------------------------------------------
// <copyright file="RedoDrawTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// D4B: <see cref="RedoDraw"/> runs Cancel → Release → preview a new resolution against the same
/// in-memory Stage, atomically from the caller's perspective (a single <c>SaveChanges</c> call in
/// <c>UseCaseExecutor.RedoDrawAsync</c>). These tests exercise the Application use case directly:
/// success, every upfront block the shared <see cref="DrawActionEvaluator"/> owns, and a scenario
/// where the upfront (single-stage) evaluator misses a block that the real Rerun launch guard
/// (full competition stages) still catches mid-sequence.
/// </summary>
public sealed class RedoDrawTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void Redo_partial_blocked_when_latecomer_has_no_free_slot()
    {
        var stage = CreateStageWithSlots(2, 2, out var draw, applyPartialFill: true);
        stage.AddResolvedPopulationEntry(EntryId.New(), _clock);
        draw = stage.Draws.Single();

        var aligned = BuildAlignedSlotKeys(stage, draw);
        SlotPartialDrawPlanner.TryGetPartialRedoBlockCode(stage, [stage], aligned, draw.Id)
            .Should().Be(ApplicationErrorCodes.DrawTooManyTeams);
        DrawActionEvaluator.Evaluate(DrawActionEvaluator.ForRedo(stage, [stage], null), draw).Redo.BlockCode
            .Should().Be(ApplicationErrorCodes.DrawTooManyTeams);

        AssertBlocked(
            () => RedoDraw.Execute(stage, draw.Id, _clock, [stage]),
            ApplicationErrorCodes.DrawTooManyTeams);
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Published);
        stage.CompositionEntries.Should().HaveCount(3);
    }

    [Fact]
    public void Redo_partial_places_a_latecomer_when_a_slot_stays_free()
    {
        var stage = CreateStageWithSlots(2, 2, out var draw, applyPartialFill: true);
        draw = stage.Draws.Single();
        stage.AddSlot("S3");
        var late = EntryId.New();
        stage.AddResolvedPopulationEntry(late, _clock);

        var result = RedoDraw.Execute(stage, draw.Id, _clock, [stage]);

        result.IsResolved.Should().BeTrue();
        stage.GetDraw(result.DrawId).InputsIntent.Should().Be(DrawInputsIntent.Partial);
        stage.GetDraw(result.DrawId).Resolution.SlotResults.Select(placement => placement.EntryId)
            .Should().Contain(late);
    }

    [Fact]
    public void Redo_succeeds_cancels_releases_and_previews_a_new_resolution()
    {
        var stage = CreateStageWithSlots(2, 2, out var draw, applyPartialFill: true);

        var result = RedoDraw.Execute(stage, draw!.Id, _clock, [stage]);

        result.DrawId.Should().NotBe(draw.Id);
        result.IsResolved.Should().BeTrue();
        result.IsNoSolution.Should().BeFalse();
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Cancelled);
        var newDraw = stage.GetDraw(result.DrawId);
        newDraw.Status.Should().Be(DrawStatus.Draft);
        newDraw.InputsIntent.Should().Be(DrawInputsIntent.Partial);
        newDraw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
        stage.Slots.Should().OnlyContain(slot => slot.EntryId == null);
    }

    [Fact]
    public void Redo_partial_succeeds_when_stage_has_a_direct_assignment()
    {
        var stage = CreateStageWithSlots(2, 2, out _);
        var entries = stage.CompositionEntries.Select(entry => entry.EntryId).ToArray();
        var manualKey = stage.Slots[1].SlotKey;
        var drawnKey = stage.Slots[0].SlotKey;
        stage.AssignEntryToSlot(manualKey, entries[1]);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ApplyPartialDrawPipelineToFillSlots(stage, draw, seed: 2);

        DrawActionEvaluator.Evaluate(DrawActionEvaluator.ForRedo(stage, [stage], null), draw).Redo.State
            .Should().Be(StageDrawActionState.Available);

        var result = RedoDraw.Execute(stage, draw.Id, _clock, [stage]);

        result.IsResolved.Should().BeTrue();
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Cancelled);
        stage.GetDraw(result.DrawId).InputsIntent.Should().Be(DrawInputsIntent.Partial);
        stage.FindSlot(manualKey)!.EntryId.Should().Be(entries[1]);
        stage.GetDraw(result.DrawId).Resolution.SlotResults
            .Should().NotContain(placement => placement.SlotKey == manualKey);
        stage.GetDraw(result.DrawId).Resolution.SlotResults
            .Should().Contain(placement => placement.SlotKey == drawnKey);
    }

    [Fact]
    public void Redo_partial_applied_with_fixed_slots_on_inputs_and_direct_assignment()
    {
        var stage = CreateStageWithSlots(2, 2, out _);
        var entries = stage.CompositionEntries.Select(entry => entry.EntryId).ToArray();
        var manualKey = stage.Slots[1].SlotKey;
        var drawnKey = stage.Slots[0].SlotKey;
        stage.AssignEntryToSlot(manualKey, entries[1]);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);

        var scope = SlotPartialDrawPlanner.Compose(stage, [stage]);
        SlotPartialDrawPlanner.EnsureDrawable(scope);
        var pool = scope.UnplacedEntries;
        pool.Should().NotBeEmpty();
        var staleFixed = new SlotDrawPlacement(pool[0], drawnKey);
        stage.ConfigureDrawInputs(
            draw.Id,
            DrawInputs.ForSlot(pool, fixedPlacements: [staleFixed]),
            DrawInputsIntent.Partial);
        stage.GetDraw(draw.Id).Inputs!.FixedSlots.Count.Should().BeGreaterThan(0);
        stage.GetDraw(draw.Id).InputsIntent.Should().Be(DrawInputsIntent.Partial);

        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            [.. stage.Slots.Select(slot => slot.SlotKey)],
            competitionStages: [stage]).IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);

        AssertRedoCommandMatchesEvaluator(stage, draw.Id, [stage], expectedBlockCode: null);
        var previewId = stage.Draws.Single(drawEntry => drawEntry.Status == DrawStatus.Draft).Id;
        stage.GetDraw(previewId).InputsIntent.Should().Be(DrawInputsIntent.Partial);
        stage.FindSlot(manualKey)!.EntryId.Should().Be(entries[1]);
        stage.GetDraw(previewId).Resolution.SlotResults
            .Should().NotContain(placement => placement.SlotKey == manualKey);
        stage.GetDraw(previewId).Inputs!.FixedSlots.Should().BeEmpty();
    }

    [Theory]
    [InlineData("partial_no_fixed")]
    [InlineData("partial_with_fixed_metadata")]
    [InlineData("default_fixed_no_da")]
    [InlineData("default_fixed_with_da")]
    public void Redo_evaluator_matches_redo_draw_block_or_success(string scenario)
    {
        switch (scenario)
        {
            case "partial_no_fixed":
            {
                var stage = CreateStageWithSlots(2, 2, out _);
                var entries = stage.CompositionEntries.Select(entry => entry.EntryId).ToArray();
                stage.AssignEntryToSlot(stage.Slots[1].SlotKey, entries[1]);
                var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
                ApplyPartialDrawPipelineToFillSlots(stage, draw, seed: 11);
                AssertRedoCommandMatchesEvaluator(stage, draw.Id, [stage], null);
                break;
            }

            case "partial_with_fixed_metadata":
            {
                var stage = CreateStageWithSlots(2, 2, out _);
                var entries = stage.CompositionEntries.Select(entry => entry.EntryId).ToArray();
                stage.AssignEntryToSlot(stage.Slots[1].SlotKey, entries[1]);
                var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
                var scope = SlotPartialDrawPlanner.Compose(stage, [stage]);
                var pool = scope.UnplacedEntries;
                stage.ConfigureDrawInputs(
                    draw.Id,
                    DrawInputs.ForSlot(pool, fixedPlacements: [new SlotDrawPlacement(pool[0], stage.Slots[0].SlotKey)]),
                    DrawInputsIntent.Partial);
                GenerateDrawResolution.Execute(
                    stage,
                    draw.Id,
                    _clock,
                    [.. stage.Slots.Select(slot => slot.SlotKey)],
                    competitionStages: [stage]);
                PublishDraw.Execute(stage, draw.Id, _clock);
                ApplyDraw.Execute(stage, draw.Id, _clock);
                AssertRedoCommandMatchesEvaluator(stage, draw.Id, [stage], null);
                break;
            }

            case "default_fixed_no_da":
            {
                var competition = CreateCompetition.Execute("Cup-rerun-parity", _clock);
                var ids = Enumerable.Range(0, 2)
                    .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
                    .ToArray();
                var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(2), _clock).Stage;
                ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
                stage.ReplaceAffectationAuthoring(ids, _clock);
                var first = PublishAndApplyDefaultSlotDraw(stage);
                CancelDraw.Execute(stage, first.Id, _clock);
                var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
                ConfigureDrawInputs.Execute(
                    stage,
                    draw.Id,
                    DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot));
                GenerateDrawResolution.Execute(
                    stage,
                    draw.Id,
                    _clock,
                    [.. stage.Slots.Select(slot => slot.SlotKey)]).IsResolved.Should().BeTrue();
                PublishDraw.Execute(stage, draw.Id, _clock);
                ApplyDraw.Execute(stage, draw.Id, _clock);
                AssertRedoCommandMatchesEvaluator(stage, draw.Id, [stage], null);
                stage.Draws.Single(drawEntry => drawEntry.Status == DrawStatus.Draft).InputsIntent
                    .Should().Be(DrawInputsIntent.Rerun);
                break;
            }

            case "default_fixed_with_da":
            {
                var competition = CreateCompetition.Execute("Cup-fixed-da", _clock);
                var ids = Enumerable.Range(0, 2)
                    .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
                    .ToArray();
                var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(2), _clock).Stage;
                ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
                stage.ReplaceAffectationAuthoring(ids, _clock);
                var first = PublishAndApplyDefaultSlotDraw(stage);
                CancelDraw.Execute(stage, first.Id, _clock);
                var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
                ConfigureDrawInputs.Execute(
                    stage,
                    draw.Id,
                    DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot));
                GenerateDrawResolution.Execute(
                    stage,
                    draw.Id,
                    _clock,
                    [.. stage.Slots.Select(slot => slot.SlotKey)]).IsResolved.Should().BeTrue();
                PublishDraw.Execute(stage, draw.Id, _clock);
                ApplyDraw.Execute(stage, draw.Id, _clock);
                var fixedSlot = stage.GetDraw(draw.Id).Inputs!.FixedSlots[0];
                stage.AssignEntryToSlot(fixedSlot.SlotKey, fixedSlot.EntryId);
                AssertRedoCommandMatchesEvaluator(
                    stage,
                    draw.Id,
                    [stage],
                    ApplicationErrorCodes.DrawLaunchDirectAssignmentPresent);
                break;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
        }
    }

    [Fact]
    public void Redo_default_with_fixed_slots_still_blocks_on_direct_assignment()
    {
        var competition = CreateCompetition.Execute("Cup-fixed", _clock);
        var ids = Enumerable.Range(0, 2)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(2), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        var first = PublishAndApplyDefaultSlotDraw(stage);
        CancelDraw.Execute(stage, first.Id, _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot));
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            [.. stage.Slots.Select(slot => slot.SlotKey)]).IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        var fixedSlots = stage.GetDraw(draw.Id).Inputs!.FixedSlots;
        fixedSlots.Count.Should().BeGreaterThan(0);
        var fixedSlot = fixedSlots[0];
        stage.AssignEntryToSlot(fixedSlot.SlotKey, fixedSlot.EntryId);
        stage.DirectAssignments.Should().ContainSingle();

        AssertBlocked(
            () => RedoDraw.Execute(stage, draw.Id, _clock, [stage]),
            ApplicationErrorCodes.DrawLaunchDirectAssignmentPresent);
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Published);
        stage.FindSlot(fixedSlot.SlotKey)!.EntryId.Should().Be(fixedSlot.EntryId);
    }

    [Fact]
    public void Redo_blocked_by_materialized_opponent_conflict_mirrors_release()
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

        AssertBlocked(
            () => RedoDraw.Execute(stage, draw.Id, _clock, [stage], [match]),
            StageErrorCodes.MaterializedOpponentConflict);
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Published);
        stage.FindSlot("S1")!.EntryId.Should().Be(home);
    }

    [Fact]
    public void Redo_blocked_when_an_applied_group_draw_has_attached_matches_on_a_ready_stage()
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
        stage.AssignEntryToGroup(group.Id, other);
        var fixture = stage.AddFixture(matchday.Id, _clock);
        var match = Match.Create(_competitionId, stage.Id, entry, other, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        stage.Prepare(_clock);

        AssertBlocked(
            () => RedoDraw.Execute(stage, draw.Id, _clock, [stage], [match]),
            ApplicationErrorCodes.DrawReleaseBlockedByAttachedMatches);
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Published);
        stage.FindGroup(group.Id)!.EntryIds.Should().Contain(entry);
    }

    [Fact]
    public void Redo_blocked_when_the_structure_is_locked()
    {
        var stage = CreateStage();
        stage.AddRound("Tour", _clock);
        stage.AddSlot("S1");
        var entry = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry], _clock);
        var draw = ResolveSlotDraw(stage, [new SlotDrawPlacement(entry, "S1")]);
        stage.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        AssertBlocked(() => RedoDraw.Execute(stage, draw.Id, _clock, [stage]), StageErrorCodes.StructureLocked);
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Published);
        stage.FindSlot("S1")!.EntryId.Should().Be(entry);
    }

    [Fact]
    public void Redo_rejects_a_draft_draw_that_was_never_applied()
    {
        var stage = CreateStage();
        stage.AddSlot("S1");
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        var entry = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry], _clock);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);

        AssertBlocked(
            () => RedoDraw.Execute(stage, draw.Id, _clock, [stage]),
            ApplicationErrorCodes.DrawRedoRequiresApplied);
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Draft);
        stage.Draws.Should().ContainSingle();
        stage.FindSlot("S1")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Redo_rejects_a_draw_already_cancelled()
    {
        var stage = CreateStage();
        stage.AddSlot("S1");
        var entry = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry], _clock);
        var draw = ResolveSlotDraw(stage, [new SlotDrawPlacement(entry, "S1")]);
        stage.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        CancelDraw.Execute(stage, draw.Id, _clock);

        AssertBlocked(
            () => RedoDraw.Execute(stage, draw.Id, _clock, [stage]),
            ApplicationErrorCodes.DrawRedoRequiresApplied);
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Cancelled);
        stage.Draws.Should().ContainSingle();

        // Cancel does not unwind materialization: the slot stays occupied, untouched by the refusal.
        stage.FindSlot("S1")!.EntryId.Should().Be(entry);
    }

    [Fact]
    public void Redo_blocked_with_DrawLaunchGridNotVacant_when_a_sibling_feed_partially_reserves_a_released_slot()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("KO"), SampleRegulations.Standard(), _clock);
        target.AddRound("R1", _clock);
        target.AddSlot("SF1-A");
        target.AddSlot("SF1-B");
        ReplaceStageDrawRules.Execute(target, new DrawRules(DrawMode.Random), _clock);
        var e1 = EntryId.New();
        var e2 = EntryId.New();
        target.ReplaceAffectationAuthoring([e1, e2], _clock);

        var draw = ResolveSlotDraw(
            target,
            [new SlotDrawPlacement(e1, "SF1-A"), new SlotDrawPlacement(e2, "SF1-B")]);
        target.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(target, draw.Id, _clock);

        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(target.Id, "SF1-A"))
            ]),
            _clock);

        var redo = DrawActionEvaluator.Evaluate(DrawActionEvaluator.ForRedo(target, [source, target], null), draw).Redo;
        redo.State.Should().Be(StageDrawActionState.Blocked);
        redo.BlockCode.Should().Be(ApplicationErrorCodes.DrawLaunchGridNotVacant);

        AssertBlocked(
            () => RedoDraw.Execute(target, draw.Id, _clock, [source, target]),
            ApplicationErrorCodes.DrawLaunchGridNotVacant);

        target.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Published);
        target.FindSlot("SF1-A")!.EntryId.Should().Be(e1);
        target.FindSlot("SF1-B")!.EntryId.Should().Be(e2);
    }

    [Fact]
    public void Redo_blocked_with_DrawNoFreeSlot_when_all_released_slots_are_reserved_by_sibling_feeds()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("KO"), SampleRegulations.Standard(), _clock);
        target.AddRound("R1", _clock);
        target.AddSlot("SF1-A");
        target.AddSlot("SF1-B");
        ReplaceStageDrawRules.Execute(target, new DrawRules(DrawMode.Random), _clock);
        var e1 = EntryId.New();
        var e2 = EntryId.New();
        target.ReplaceAffectationAuthoring([e1, e2], _clock);

        var draw = ResolveSlotDraw(
            target,
            [new SlotDrawPlacement(e1, "SF1-A"), new SlotDrawPlacement(e2, "SF1-B")]);
        target.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(target, draw.Id, _clock);

        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(target.Id, "SF1-A")),
                new QualificationPath(
                    2,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 2),
                    QualificationDestination.ForSlot(target.Id, "SF1-B")),
            ]),
            _clock);

        var redo = DrawActionEvaluator.Evaluate(DrawActionEvaluator.ForRedo(target, [source, target], null), draw).Redo;
        redo.State.Should().Be(StageDrawActionState.Blocked);
        redo.BlockCode.Should().Be(ApplicationErrorCodes.DrawNoFreeSlot);

        AssertBlocked(
            () => RedoDraw.Execute(target, draw.Id, _clock, [source, target]),
            ApplicationErrorCodes.DrawNoFreeSlot);

        target.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Published);
    }

    [Fact]
    public void Redo_default_with_two_fixed_slots_previews_rerun_when_no_direct_assignment()
    {
        var competition = CreateCompetition.Execute("Cup-rerun", _clock);
        var ids = Enumerable.Range(0, 2)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(2), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        var first = PublishAndApplyDefaultSlotDraw(stage);
        CancelDraw.Execute(stage, first.Id, _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot));
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            [.. stage.Slots.Select(slot => slot.SlotKey)]).IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.GetDraw(draw.Id).Inputs!.FixedSlots.Count.Should().Be(2);
        stage.DirectAssignments.Should().BeEmpty();

        var result = RedoDraw.Execute(stage, draw.Id, _clock, [stage]);

        result.IsResolved.Should().BeTrue();
        stage.GetDraw(result.DrawId).InputsIntent.Should().Be(DrawInputsIntent.Rerun);
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Cancelled);
        stage.GetDraw(result.DrawId).Resolution.SlotResults.Should().NotBeEmpty();
    }

    [Fact]
    public void Redo_partial_available_with_a_single_drawn_slot_and_succeeds()
    {
        var stage = CreateStageWithSlots(2, 2, out _);
        var entries = stage.CompositionEntries.Select(entry => entry.EntryId).ToArray();
        stage.AssignEntryToSlot(stage.Slots[1].SlotKey, entries[1]);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ApplyPartialDrawPipelineToFillSlots(stage, draw, seed: 3);

        DrawActionEvaluator.Evaluate(DrawActionEvaluator.ForRedo(stage, [stage], null), draw).Redo.State
            .Should().Be(StageDrawActionState.Available);

        var result = RedoDraw.Execute(stage, draw.Id, _clock, [stage]);
        result.IsResolved.Should().BeTrue();
        stage.GetDraw(result.DrawId).InputsIntent.Should().Be(DrawInputsIntent.Partial);
    }

    [Fact]
    public void Redo_partial_succeeds_after_direct_assignment_was_cleared()
    {
        var stage = CreateStageWithSlots(2, 2, out _);
        var entries = stage.CompositionEntries.Select(entry => entry.EntryId).ToArray();
        var manualKey = stage.Slots[1].SlotKey;
        stage.AssignEntryToSlot(manualKey, entries[1]);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ApplyPartialDrawPipelineToFillSlots(stage, draw, seed: 4);
        stage.ClearSlotAssignment(manualKey);

        var result = RedoDraw.Execute(stage, draw.Id, _clock, [stage]);

        result.IsResolved.Should().BeTrue();
        stage.GetDraw(result.DrawId).Resolution.SlotResults
            .Should().Contain(placement => placement.SlotKey == manualKey);
    }

    [Fact]
    public void Redo_partial_blocked_when_a_drawn_entry_left_composition()
    {
        var stage = CreateStageWithSlots(2, 2, out var draw, applyPartialFill: true);
        draw = stage.Draws.Single();
        var remaining = stage.CompositionEntries.Select(entry => entry.EntryId).Take(1).ToArray();
        stage.ReplaceAffectationAuthoring(remaining, _clock);

        AssertBlocked(
            () => RedoDraw.Execute(stage, draw.Id, _clock, [stage]),
            ApplicationErrorCodes.DrawNotEnoughTeams);
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Published);
    }

    [Fact]
    public void Redo_partial_blocked_with_materialized_opponent_when_da_and_draw_share_an_attached_pair()
    {
        var stage = CreateStage();
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.AddRound("R1", _clock);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        stage.SeedEntryRoundBracketPairs();
        var daEntry = EntryId.New();
        var drawnEntry = EntryId.New();
        stage.ReplaceAffectationAuthoring([daEntry, drawnEntry], _clock);
        stage.AssignEntryToSlot("S1", daEntry);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ApplyPartialDrawPipelineToFillSlots(stage, draw, seed: 1);

        var round = stage.Rounds.Single();
        var fixture = stage.AddFixture(round.Id, _clock, "S1", "S2", "P1");
        var match = Match.Create(_competitionId, stage.Id, daEntry, drawnEntry, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);

        AssertBlocked(
            () => RedoDraw.Execute(stage, draw.Id, _clock, [stage], [match]),
            StageErrorCodes.MaterializedOpponentConflict);
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Published);
        stage.FindSlot("S1")!.EntryId.Should().Be(daEntry);
        stage.FindSlot("S2")!.EntryId.Should().Be(drawnEntry);
    }

    [Fact]
    public void Redo_partial_generation_targets_differ_without_competition_stages()
    {
        var stage = CreateStageWithSlots(2, 2, out _);
        var entries = stage.CompositionEntries.Select(entry => entry.EntryId).ToArray();
        var vacantKey = stage.Slots[0].SlotKey;
        stage.AssignEntryToSlot(stage.Slots[1].SlotKey, entries[1]);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ApplyPartialDrawPipelineToFillSlots(stage, draw, seed: 6);

        var result = RedoDraw.Execute(stage, draw.Id, _clock, [stage]);
        stage.GetDraw(result.DrawId).Resolution.SlotResults
            .Select(placement => placement.SlotKey)
            .Should().ContainSingle()
            .Which.Should().Be(vacantKey);

        CancelDraw.Execute(stage, result.DrawId, _clock);
        var wideDraft = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            wideDraft.Id,
            stage.GetDraw(result.DrawId).Inputs!,
            DrawInputsIntent.Partial,
            [stage]);
        var act = () => GenerateDrawResolution.Execute(
            stage,
            wideDraft.Id,
            _clock,
            [.. stage.Slots.Select(slot => slot.SlotKey)]);

        act.Should().Throw<DomainException>()
            .WithMessage("*free entries must equal free targets*");

        GenerateDrawResolution.Execute(
            stage,
            wideDraft.Id,
            _clock,
            [.. stage.Slots.Select(slot => slot.SlotKey)],
            competitionStages: [stage]).IsResolved.Should().BeTrue();
        stage.GetDraw(wideDraft.Id).Resolution.SlotResults
            .Should().ContainSingle(placement => placement.SlotKey == vacantKey);
    }

    [Fact]
    public void Redo_groups_keeps_manual_placements_and_relaunches_default()
    {
        var (_, stage, ids) = AppliedGroups();
        var applied = stage.Draws.Single();
        var drawn = GroupDrawLaunchRules.DrawnEntries(applied);

        var result = RedoDraw.Execute(stage, applied.Id, _clock, [stage]);

        stage.GetDraw(applied.Id).Status.Should().Be(DrawStatus.Cancelled);
        stage.Groups.SelectMany(group => group.EntryIds).Should().Contain(ids[0]);
        stage.Groups.SelectMany(group => group.EntryIds).Should().NotIntersectWith(drawn);
        var preview = stage.GetDraw(result.DrawId);
        preview.InputsIntent.Should().Be(DrawInputsIntent.Default);
        preview.Inputs!.FixedGroups.Select(placement => placement.EntryId).Should().Contain(ids[0]);
        preview.Resolution.State.Should().Be(DrawResolutionState.Resolved);
    }

    [Fact]
    public void Redo_groups_refuses_an_invalid_shape_before_cancel()
    {
        var (_, stage, ids) = AppliedGroups();
        var extra = EntryId.New();
        stage.ReplaceAffectationAuthoring([.. ids, extra], _clock);
        var applied = stage.Draws.Single();
        var before = stage.Groups.Select(group => group.EntryIds.ToArray()).ToArray();

        var act = () => RedoDraw.Execute(stage, applied.Id, _clock, [stage]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawGroupShapeInvalid);
        stage.GetDraw(applied.Id).Status.Should().Be(DrawStatus.Published);
        stage.Draws.Should().ContainSingle();
        stage.Groups.SelectMany(group => group.EntryIds).Should().Equal(before.SelectMany(entryIds => entryIds));
    }

    [Fact]
    public void Redo_groups_evaluator_is_available_with_fixed_occupants()
    {
        var (_, stage, _) = AppliedGroups();
        var draw = stage.Draws.Single();

        DrawActionEvaluator.Evaluate(DrawActionEvaluator.ForCommand(stage, null), draw).Redo.State
            .Should().Be(StageDrawActionState.Available);
    }

    [Fact]
    public void R5_delete_then_latecomer_blocks_redo_until_cancel_release_and_default()
    {
        var (competition, stage, _) = AppliedGroups();
        var applied = stage.Draws.Single();
        var fixedIds = applied.Inputs!.FixedGroups.Select(placement => placement.EntryId).ToHashSet();
        var removed = applied.Resolution.GroupResults
            .Select(placement => placement.EntryId)
            .First(entryId => !fixedIds.Contains(entryId));
        DeleteEntry.Execute(competition, removed, [stage], [], new Moq.Mock<IMatchRepository>().Object, _clock);
        var late = AddEntry.Execute(competition, "Late", _clock).Id;
        var remaining = stage.CompositionEntries.Select(entry => entry.EntryId).ToArray();
        stage.ReplaceAffectationAuthoring([.. remaining, late], _clock);

        var redo = () => RedoDraw.Execute(stage, applied.Id, _clock, [stage]);
        redo.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawRedoRequiresApplied);
        stage.GetDraw(applied.Id).Status.Should().Be(DrawStatus.Published);
        stage.Draws.Should().ContainSingle();

        CancelDraw.Execute(stage, applied.Id, _clock);
        ReleaseDrawAlignedPlacements.Execute(stage, applied.Id, _clock);
        var created = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock, competitionStages: [stage]);
        ConfigureDrawInputs.Execute(
            stage,
            created.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group),
            competitionStages: [stage]);

        var inputs = stage.GetDraw(created.Id).Inputs!;
        inputs.Entries.Should().Contain(late);
        inputs.Entries.Should().NotContain(removed);
        inputs.PotMembership!.Pots.Should().Equal(
            GroupDrawPots.Compute([.. inputs.Entries], 2));
    }

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

    private static HashSet<string> BuildAlignedSlotKeys(Stage stage, Draw draw)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        if (draw.Resolution.State != DrawResolutionState.Resolved)
        {
            return keys;
        }

        foreach (var placement in draw.Resolution.SlotResults)
        {
            if (stage.DirectAssignments.Any(assignment =>
                    string.Equals(assignment.SlotKey, placement.SlotKey, StringComparison.Ordinal)))
            {
                continue;
            }

            var slot = stage.FindSlot(placement.SlotKey);
            if (slot?.EntryId is { } current && current.Equals(placement.EntryId))
            {
                keys.Add(slot.SlotKey);
            }
        }

        return keys;
    }

    private void AssertRedoCommandMatchesEvaluator(
        Stage stage,
        DrawId drawId,
        IReadOnlyList<Stage> competitionStages,
        string? expectedBlockCode)
    {
        var draw = stage.GetDraw(drawId);
        var redo = DrawActionEvaluator.Evaluate(
            DrawActionEvaluator.ForRedo(stage, competitionStages, null),
            draw).Redo;
        if (expectedBlockCode is null)
        {
            redo.State.Should().Be(StageDrawActionState.Available);
            redo.BlockCode.Should().BeNull();
            RedoDraw.Execute(stage, drawId, _clock, competitionStages).IsResolved.Should().BeTrue();
            return;
        }

        redo.State.Should().Be(StageDrawActionState.Blocked);
        redo.BlockCode.Should().Be(expectedBlockCode);
        AssertBlocked(
            () => RedoDraw.Execute(stage, drawId, _clock, competitionStages),
            expectedBlockCode);
    }

    private void ApplyDrawPipelineToFillSlots(Stage stage, Draw draw)
    {
        var defaultInputs = DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot);
        ConfigureDrawInputs.Execute(stage, draw.Id, defaultInputs);
        var slotTargets = stage.Slots.Select(slot => slot.SlotKey).ToArray();
        GenerateDrawResolution.Execute(stage, draw.Id, _clock, slotTargets);
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.Slots.Should().OnlyContain(slot => slot.EntryId != null);
    }

    private void ApplyPartialDrawPipelineToFillSlots(Stage stage, Draw draw, int? seed = null)
    {
        var inputs = DrawInputsFactory.CreatePartial(stage, [stage], DrawResolutionKind.Slot);
        ConfigureDrawInputs.Execute(stage, draw.Id, inputs, DrawInputsIntent.Partial, [stage]);
        var slotTargets = stage.Slots.Select(slot => slot.SlotKey).ToArray();
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            slotTargets,
            randomSource: seed is int value ? new SeededRandomSource(value) : null,
            competitionStages: [stage]);
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
    }

    private Stage CreateStageWithSlots(int slots, int teams, out Draw? draw, bool applyPartialFill = false)
    {
        var stage = CreateStage();
        for (var index = 0; index < slots; index++)
        {
            stage.AddSlot($"S{index + 1}");
        }

        var entries = Enumerable.Range(0, teams).Select(_ => EntryId.New()).ToArray();
        stage.ReplaceAffectationAuthoring(entries, _clock);
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        draw = null;
        if (applyPartialFill)
        {
            draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
            ApplyPartialDrawPipelineToFillSlots(stage, draw);
        }

        return stage;
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

    private (Competition Competition, Stage Stage, EntryId[] Ids) AppliedGroups()
    {
        var competition = CreateCompetition.Execute("Redo groups", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 2), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random, potRules: new PotRules(2)), _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock, competitionStages: [stage]);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group),
            competitionStages: [stage]);
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [.. stage.Groups.Select(group => group.Id)]).IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        return (competition, stage, ids);
    }

    [Fact]
    public void Redo_default_fixed_succeeds_when_an_older_cancelled_draw_still_has_no_releasable_targets()
    {
        var competition = CreateCompetition.Execute("Cup-rerun-last-author", _clock);
        var ids = Enumerable.Range(0, 2)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(2), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        var older = PublishAndApplyDefaultSlotDraw(stage);
        CancelDraw.Execute(stage, older.Id, _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot));
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            [.. stage.Slots.Select(slot => slot.SlotKey)]).IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);

        var result = RedoDraw.Execute(stage, draw.Id, _clock, [stage]);

        result.IsResolved.Should().BeTrue();
        stage.GetDraw(result.DrawId).InputsIntent.Should().Be(DrawInputsIntent.Rerun);
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Cancelled);
    }

    private Draw PublishAndApplyDefaultSlotDraw(Stage stage)
    {
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot));
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            [.. stage.Slots.Select(slot => slot.SlotKey)]).IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        return stage.GetDraw(draw.Id);
    }
}
