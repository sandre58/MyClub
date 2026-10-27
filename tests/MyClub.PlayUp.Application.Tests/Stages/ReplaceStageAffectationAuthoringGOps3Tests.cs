// -----------------------------------------------------------------------
// <copyright file="ReplaceStageAffectationAuthoringGOps3Tests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// <see cref="ReplaceStageAffectationAuthoring"/> when a draw is Applied.
/// </summary>
public sealed class ReplaceStageAffectationAuthoringGOps3Tests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 15, 16, 0, 0, TimeSpan.Zero));

    [Fact]
    public void ReplaceAffectationAuthoring_allows_shrink_when_group_draw_published_not_applied()
    {
        var (competition, stage, drawId, ids) = CreateGroupStageWithPublishedDraw(applied: false);
        stage.Status.Should().Be(StageStatus.Draft);
        DrawAppliedState.IsApplied(stage.GetDraw(drawId), stage).Should().BeFalse();

        ReplaceStageAffectationAuthoring.Execute(
            stage,
            competition,
            [ids[0], ids[1], ids[2]],
            _clock,
            [stage],
            []);

        stage.AffectationAuthoring.Select(e => e.EntryId).Should().HaveCount(3);
    }

    [Fact]
    public void ReplaceAffectationAuthoring_allows_replace_with_same_entries_when_group_draw_applied()
    {
        var (competition, stage, drawId, ids) = CreateGroupStageWithPublishedDraw(applied: true);
        PrepareStage.Execute(stage, [stage], _clock);
        DrawAppliedState.IsApplied(stage.GetDraw(drawId), stage).Should().BeTrue();

        ReplaceStageAffectationAuthoring.Execute(
            stage,
            competition,
            ids,
            _clock,
            [stage],
            []);

        stage.AffectationAuthoring.Select(e => e.EntryId).Should().Equal(ids);
    }

    [Fact]
    public void ReplaceAffectationAuthoring_allows_change_when_draw_cancelled()
    {
        var (competition, stage, drawId, ids) = CreateGroupStageWithPublishedDraw(applied: true);
        stage.CancelDraw(drawId, _clock);
        stage.GetDraw(drawId).Status.Should().Be(DrawStatus.Cancelled);
        DrawAppliedState.IsApplied(stage.GetDraw(drawId), stage).Should().BeFalse();
        PrepareStage.Execute(stage, [stage], _clock);

        ReplaceStageAffectationAuthoring.Execute(
            stage,
            competition,
            [ids[0], ids[1]],
            _clock,
            [stage],
            []);

        stage.AffectationAuthoring.Should().HaveCount(2);
    }

    [Fact]
    public void ReplaceAffectationAuthoring_refuses_shrink_when_group_draw_applied()
    {
        var (competition, stage, drawId, ids) = CreateGroupStageWithPublishedDraw(applied: true);
        PrepareStage.Execute(stage, [stage], _clock);
        DrawAppliedState.IsApplied(stage.GetDraw(drawId), stage).Should().BeTrue();

        var act = () => ReplaceStageAffectationAuthoring.Execute(
            stage,
            competition,
            [ids[0], ids[1], ids[2]],
            _clock,
            [stage],
            []);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AppliedDrawPoolChangeBlocked);
        stage.AffectationAuthoring.Select(e => e.EntryId).Should().Equal(ids);
    }

    [Fact]
    public void ReplaceAffectationAuthoring_refuses_swap_with_same_count_when_group_draw_applied()
    {
        var (competition, stage, drawId, ids) = CreateGroupStageWithPublishedDraw(applied: true);
        var replacement = AddEntry.Execute(competition, "Replacement", _clock).Id;
        PrepareStage.Execute(stage, [stage], _clock);
        DrawAppliedState.IsApplied(stage.GetDraw(drawId), stage).Should().BeTrue();

        var proposed = new[] { ids[0], ids[1], ids[2], replacement };

        var act = () => ReplaceStageAffectationAuthoring.Execute(
            stage,
            competition,
            proposed,
            _clock,
            [stage],
            []);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AppliedDrawPoolChangeBlocked);
        stage.AffectationAuthoring.Select(e => e.EntryId).Should().Equal(ids);
    }

    private (Competition Competition, Stage Stage, DrawId DrawId, EntryId[] EntryIds)
        CreateGroupStageWithPublishedDraw(bool applied)
    {
        var competition = CreateCompetition.Execute("G-OPS3-draw", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(i => AddEntry.Execute(competition, $"T{i}", _clock).Id)
            .ToArray();

        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock).Stage;
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group));
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [.. stage.Groups.Select(g => g.Id)]).IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);

        if (applied)
        {
            ApplyDraw.Execute(stage, draw.Id, _clock);
        }

        return (competition, stage, draw.Id, ids);
    }
}
