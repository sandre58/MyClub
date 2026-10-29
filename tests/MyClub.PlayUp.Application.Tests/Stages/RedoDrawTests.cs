// -----------------------------------------------------------------------
// <copyright file="RedoDrawTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
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
}
