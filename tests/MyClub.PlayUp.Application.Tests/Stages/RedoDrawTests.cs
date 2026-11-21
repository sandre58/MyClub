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
    public void Redo_succeeds_when_composition_gained_extra_entries_after_apply()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var entries = Enumerable.Range(0, 2)
            .Select(i => AddEntry.Execute(competition, $"T{i}", _clock).Id)
            .ToArray();
        var configured = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(2), _clock);
        var stage = configured.Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring([.. entries], _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ApplyDrawPipelineToFillSlots(stage, draw);

        stage.AddResolvedPopulationEntry(EntryId.New(), _clock);

        var result = RedoDraw.Execute(stage, draw.Id, _clock, [stage]);

        result.IsResolved.Should().BeTrue();
        stage.GetDraw(result.DrawId).Resolution.State.Should().Be(DrawResolutionState.Resolved);
        stage.CompositionEntries.Should().HaveCount(3);
    }

    [Fact]
    public void Redo_succeeds_cancels_releases_and_previews_a_new_resolution()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var entries = Enumerable.Range(0, 2)
            .Select(i => AddEntry.Execute(competition, $"T{i}", _clock).Id)
            .ToArray();
        var configured = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(2), _clock);
        var stage = configured.Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring([.. entries], _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ApplyDrawPipelineToFillSlots(stage, draw);

        var result = RedoDraw.Execute(stage, draw.Id, _clock, [stage]);

        result.DrawId.Should().NotBe(draw.Id);
        result.IsResolved.Should().BeTrue();
        result.IsNoSolution.Should().BeFalse();
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Cancelled);
        var newDraw = stage.GetDraw(result.DrawId);
        newDraw.Status.Should().Be(DrawStatus.Draft);
        newDraw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
        stage.Slots.Should().OnlyContain(slot => slot.EntryId == null);
    }

    [Fact]
    public void Redo_blocked_when_stage_has_a_direct_assignment()
    {
        var stage = CreateStage();
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        var entry = EntryId.New();
        var other = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry, other], _clock);
        var draw = ResolveSlotDraw(stage, [new SlotDrawPlacement(entry, "S1")]);
        stage.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.AssignEntryToSlot("S2", other);

        AssertBlocked(
            () => RedoDraw.Execute(stage, draw.Id, _clock, [stage]),
            ApplicationErrorCodes.DrawLaunchDirectAssignmentPresent);
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Published);
        stage.FindSlot("S1")!.EntryId.Should().Be(entry);
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
    public void Redo_surfaces_a_sibling_feed_the_upfront_single_stage_evaluator_cannot_see()
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

        // The upfront evaluator only has the single Stage (DrawActionContext.ForCommand) — it never
        // sees the sibling `source` stage's feed, so it reports the redo as Available.
        DrawActionEvaluator.Evaluate(DrawActionEvaluator.ForCommand(target, null), draw).Redo.State
            .Should().Be(StageDrawActionState.Available);

        var act = () => RedoDraw.Execute(target, draw.Id, _clock, [source, target]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawLaunchGridNotVacant);

        // Cancel + Release already ran in-memory on this Stage instance by the time the real guard
        // (fed the full competition stages) throws. Only the caller never reaching its single
        // SaveChanges — proven at the Host level — keeps this half-sequence from persisting.
        target.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Cancelled);
        target.FindSlot("SF1-A")!.EntryId.Should().BeNull();
        target.FindSlot("SF1-B")!.EntryId.Should().BeNull();
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
}
