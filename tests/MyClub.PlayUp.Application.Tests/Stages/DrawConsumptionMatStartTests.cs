// -----------------------------------------------------------------------
// <copyright file="DrawConsumptionMatStartTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// Materialize and Start consumption gates, and read parity.
/// </summary>
public sealed class DrawConsumptionMatStartTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void MaterializeMatches_R_Inv_1_denies_when_draw_obsolete_after_allowed_pool_shrink()
    {
        var (competition, stage, drawId, ids) = CreateGroupsWithPublishedDraw();
        PrepareStage.Execute(stage, [stage], _clock);
        stage.ReplaceAffectationAuthoringOperationally([ids[0], ids[1], ids[2]], _clock);
        DrawConsumptionConformance.IsObsoleteForConsumption(stage.GetDraw(drawId), stage).Should().BeTrue();

        var act = () => MaterializeMatches.Execute(competition, stage, [], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawObsoleteForConsumption);
    }

    [Fact]
    public void Start_R_Inv_1_denies_when_draw_obsolete_after_materialized()
    {
        var (competition, stage, _, ids) = CreateGroupsWithPublishedDraw();
        PrepareStage.Execute(stage, [stage], _clock);
        var summaries = StageStartReadinessGuard.ToSummaryRows(
            MaterializeMatches.Execute(competition, stage, [], _clock).CreatedMatches);

        stage.ReplaceAffectationAuthoringOperationally([ids[0], ids[1], ids[2]], _clock);

        var act = () => StageStartReadinessGuard.EnsureReadyToStart(stage, competition, summaries);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawObsoleteForConsumption);
    }

    [Fact]
    public void Read_materialize_disabled_reason_when_draw_obsolete()
    {
        var (competition, stage, drawId, ids) = CreateGroupsWithPublishedDraw();
        PrepareStage.Execute(stage, [stage], _clock);
        stage.ReplaceAffectationAuthoringOperationally([ids[0], ids[1], ids[2]], _clock);
        DrawConsumptionConformance.IsObsoleteForConsumption(stage.GetDraw(drawId), stage).Should().BeTrue();

        var view = StructureViewAssembler.Assemble(competition, [stage]);
        var materialize = view.Stages.Single().Materialize;

        materialize.Should().NotBeNull();
        materialize.CommandEnabled.Should().BeFalse();
        materialize.DisabledReason.Should().Be(StructureMaterializeDisabledReasons.DrawObsoleteForConsumption);
    }

    [Fact]
    public void MaterializeCupFromOccupiedSlots_denies_when_draw_obsolete_on_stage()
    {
        var competition = CreateCompetition.Execute("cup-mat-start", _clock);
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(4),
            _clock).Stage;
        stage.AddSlot("SF1-A");
        stage.AddSlot("SF1-B");
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        var ids = Enumerable.Range(0, 2)
            .Select(i => AddEntry.Execute(competition, $"T{i}", _clock).Id)
            .ToArray();
        stage.ReplaceAffectationAuthoring(ids, _clock);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(stage, draw.Id, DrawInputs.ForSlot(ids));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(ids[0], "SF1-A"),
                new SlotDrawPlacement(ids[1], "SF1-B")
            ]),
            _clock);
        PublishDraw.Execute(stage, draw.Id, _clock);
        stage.SeedEntryRoundBracketPairs();
        stage.ApplyResolvedEntry("SF1-A", ids[0], _clock);
        stage.ApplyResolvedEntry("SF1-B", EntryId.New(), _clock);
        PrepareStage.Execute(stage, [stage], _clock);
        stage.ReplaceAffectationAuthoringOperationally([ids[0]], _clock);

        var act = () => MaterializeCupFromOccupiedSlots.Execute(competition, stage, ["P1"], [], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawObsoleteForConsumption);
    }

    [Fact]
    public void Read_start_disabled_reason_when_draw_obsolete_after_materialized()
    {
        var (competition, stage, _, ids) = CreateGroupsWithPublishedDraw();
        PrepareStage.Execute(stage, [stage], _clock);
        var summaries = StageStartReadinessGuard.ToSummaryRows(
            MaterializeMatches.Execute(competition, stage, [], _clock).CreatedMatches);
        stage.ReplaceAffectationAuthoringOperationally([ids[0], ids[1], ids[2]], _clock);

        var view = StructureViewAssembler.Assemble(
            competition,
            [stage],
            matchSummariesByStage: new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
            {
                [stage.Id] = summaries
            });
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledDrawObsoleteForConsumption);
    }

    private (Competition Competition, Stage Stage, DrawId DrawId, EntryId[] EntryIds) CreateGroupsWithPublishedDraw()
    {
        var competition = CreateCompetition.Execute("mat-start", _clock);
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

        var groupA = stage.Groups[0];
        var groupB = stage.Groups[1];
        stage.AssignEntryToGroup(groupA.Id, ids[0]);
        stage.AssignEntryToGroup(groupA.Id, ids[1]);
        stage.AssignEntryToGroup(groupB.Id, ids[2]);
        stage.AssignEntryToGroup(groupB.Id, ids[3]);

        return (competition, stage, draw.Id, ids);
    }
}
