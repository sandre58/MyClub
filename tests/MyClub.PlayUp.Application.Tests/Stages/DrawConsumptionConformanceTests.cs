// -----------------------------------------------------------------------
// <copyright file="DrawConsumptionConformanceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// SLICE-C-ENF-DETECT — predicate proofs (no Apply/Materialize/Start gates).
/// </summary>
public sealed class DrawConsumptionConformanceTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Published_resolved_draw_obsolete_when_effective_pool_shrinks_after_allowed_mutation()
    {
        var (stage, drawId, ids) = CreateGroupStageWithPublishedDraw();
        stage.ReplaceAffectationAuthoring([ids[0], ids[1], ids[2]], _clock);

        var draw = stage.GetDraw(drawId);
        DrawConsumptionConformance.IsObsoleteForConsumption(draw, stage).Should().BeTrue();
    }

    [Fact]
    public void Published_resolved_with_composition_only_when_affectation_empty_is_not_obsolete()
    {
        var competition = CreateCompetition.Execute("composition-only", _clock);
        var ids = Enumerable.Range(0, 2)
            .Select(i => AddEntry.Execute(competition, $"T{i}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(2),
            _clock).Stage;
        stage.AddSlot("A");
        stage.AddSlot("B");
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.AddResolvedPopulationEntry(ids[0], _clock);
        stage.AddResolvedPopulationEntry(ids[1], _clock);
        stage.AffectationAuthoring.Should().BeEmpty();

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(stage, draw.Id, DrawInputs.ForSlot(ids));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(ids[0], "A"),
                new SlotDrawPlacement(ids[1], "B"),
            ]),
            _clock);
        PublishDraw.Execute(stage, draw.Id, _clock);

        DrawConsumptionConformance.IsObsoleteForConsumption(draw, stage).Should().BeFalse();
    }

    [Fact]
    public void Published_resolved_when_inputs_pool_and_resolution_align_is_not_obsolete()
    {
        var (stage, drawId, _) = CreateGroupStageWithPublishedDraw();
        var draw = stage.GetDraw(drawId);

        DrawConsumptionConformance.IsObsoleteForConsumption(draw, stage).Should().BeFalse();
    }

    [Fact]
    public void Removed_fixed_slot_reference_is_obsolete()
    {
        var competition = CreateCompetition.Execute("fixed-slot", _clock);
        var home = AddEntry.Execute(competition, "H", _clock).Id;
        var away = AddEntry.Execute(competition, "A", _clock).Id;
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(4),
            _clock).Stage;
        stage.AddSlot("SF1-A");
        stage.AddSlot("SF1-B");
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring([home, away], _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputs.ForSlot(
                [home, away],
                fixedPlacements: [new SlotDrawPlacement(home, "SF1-A")]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(home, "SF1-A"),
                new SlotDrawPlacement(away, "SF1-B"),
            ]),
            _clock);
        PublishDraw.Execute(stage, draw.Id, _clock);

        stage.RemoveSlot("SF1-A");

        DrawConsumptionConformance.IsObsoleteForConsumption(draw, stage).Should().BeTrue();
    }

    [Fact]
    public void Empty_effective_pool_with_resolved_entries_is_obsolete()
    {
        var competition = CreateCompetition.Execute("empty-pool", _clock);
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

        var draw = PublishGroupDrawOnStage(stage);
        stage.ClearCompositionEntries(_clock);

        DrawConsumptionConformance.IsObsoleteForConsumption(draw, stage).Should().BeTrue();
    }

    [Fact]
    public void Same_entry_set_reordered_in_affectation_is_not_obsolete()
    {
        var (stage, drawId, ids) = CreateGroupStageWithPublishedDraw();
        stage.ReplaceAffectationAuthoring([ids[3], ids[2], ids[1], ids[0]], _clock);

        var draw = stage.GetDraw(drawId);
        DrawConsumptionConformance.IsObsoleteForConsumption(draw, stage).Should().BeFalse();
    }

    [Fact]
    public void Applied_draw_is_not_obsolete()
    {
        var (stage, drawId, _) = CreateGroupStageWithPublishedDraw();
        ApplyDraw.Execute(stage, drawId, _clock);

        var draw = stage.GetDraw(drawId);
        DrawConsumptionConformance.IsObsoleteForConsumption(draw, stage).Should().BeFalse();
    }

    [Fact]
    public void Draft_draw_is_not_obsolete()
    {
        var stage = CreateEmptyGroupStage();
        var draft = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        DrawConsumptionConformance.IsObsoleteForConsumption(draft, stage).Should().BeFalse();
    }

    [Fact]
    public void Cancelled_draw_is_not_obsolete()
    {
        var (stage, drawId, _) = CreateGroupStageWithPublishedDraw();
        var published = stage.GetDraw(drawId);
        stage.CancelDraw(published.Id, _clock);
        DrawConsumptionConformance.IsObsoleteForConsumption(published, stage).Should().BeFalse();
    }

    [Fact]
    public void NoSolution_draw_is_not_obsolete()
    {
        var stage = CreateEmptyGroupStage();
        var noSolutionDraw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            noSolutionDraw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group));
        stage.MarkDrawNoSolution(noSolutionDraw.Id, _clock);
        DrawConsumptionConformance.IsObsoleteForConsumption(noSolutionDraw, stage).Should().BeFalse();
    }

    [Fact]
    public void Resolved_draft_draw_is_not_obsolete()
    {
        var stage = CreateEmptyGroupStage();
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
        DrawConsumptionConformance.IsObsoleteForConsumption(draw, stage).Should().BeFalse();
    }

    private Stage CreateEmptyGroupStage()
    {
        var competition = CreateCompetition.Execute("state", _clock);
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
        return stage;
    }

    private (Stage Stage, DrawId DrawId, EntryId[] EntryIds) CreateGroupStageWithPublishedDraw()
    {
        var competition = CreateCompetition.Execute("detect", _clock);
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
        var draw = PublishGroupDrawOnStage(stage);
        return (stage, draw.Id, ids);
    }

    private Draw PublishGroupDrawOnStage(Stage stage)
    {
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
        return draw;
    }
}
