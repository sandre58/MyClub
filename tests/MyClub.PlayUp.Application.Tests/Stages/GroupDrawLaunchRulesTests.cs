// -----------------------------------------------------------------------
// <copyright file="GroupDrawLaunchRulesTests.cs" company="Stéphane ANDRE">
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
/// Shared Groups launch rule: first cause, gate parity, and a refused create leaves no draft.
/// </summary>
public sealed class GroupDrawLaunchRulesTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Empty_grid_with_a_drawable_shape_is_allowed()
    {
        var (_, stage, _) = OpenGroups();

        var decision = GroupDrawLaunchRules.Evaluate(stage, [stage]);

        decision.IsAllowed.Should().BeTrue();
        decision.FixedGroups.Should().BeEmpty();
        decision.Pots.Should().NotBeNull();
    }

    [Fact]
    public void Two_occupants_from_different_pots_are_fixed()
    {
        var (_, stage, ids) = OpenGroups();
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[2]);

        var decision = GroupDrawLaunchRules.Evaluate(stage, [stage]);

        decision.IsAllowed.Should().BeTrue();
        decision.FixedGroups.Should().BeEquivalentTo(
        [
            new GroupDrawPlacement(ids[0], stage.Groups[0].Id),
            new GroupDrawPlacement(ids[2], stage.Groups[0].Id)
        ]);
    }

    [Fact]
    public void Same_pot_in_one_group_is_refused_and_create_leaves_no_draft()
    {
        var (competition, stage, ids) = OpenGroups();
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[1]);

        var decision = GroupDrawLaunchRules.Evaluate(stage, [stage]);
        decision.IsAllowed.Should().BeFalse();
        decision.Code.Should().Be(ApplicationErrorCodes.DrawFixedPotConflict);
        decision.Reasons.Should().ContainSingle().Which.Should().Contain("pot:1");

        var hub = StructureViewAssembler.Assemble(competition, [stage]).Stages.Single();
        hub.DrawCreateLaunch.Allowed.Should().BeFalse();
        hub.DrawCreateLaunch.BlockReasonCode.Should().Be(ApplicationErrorCodes.DrawFixedPotConflict);
        hub.DrawCreateLaunch.BlockDetail.Should().Equal(decision.Reasons);

        var act = () => CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock, competitionStages: [stage]);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawFixedPotConflict);
        stage.Draws.Should().BeEmpty();
    }

    [Fact]
    public void Pending_inbound_feed_is_refused_before_a_draft_exists()
    {
        var (competition, stage, _) = OpenGroups();
        var source = Stage.Create(competition.Id, new StageName("Source"), SampleRegulations.Standard(), _clock);
        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForGroup(stage.Id, stage.Groups[0].Id))
            ]),
            _clock);

        var stages = new Stage[] { source, stage };
        var decision = GroupDrawLaunchRules.Evaluate(stage, stages);
        decision.Code.Should().Be(ApplicationErrorCodes.DrawGroupSeatsReservedByInboundFeed);
        decision.Reasons.Should().ContainSingle()
            .Which.Should().Be($"group:{stage.Groups[0].Id.Value:D}:reserved:1");

        var hub = StructureViewAssembler.Assemble(competition, stages).Stages.Single(item => item.StageId == stage.Id.Value);
        hub.DrawCreateLaunch.BlockReasonCode.Should().Be(ApplicationErrorCodes.DrawGroupSeatsReservedByInboundFeed);
        hub.DrawCreateLaunch.BlockDetail.Should().Equal(decision.Reasons);

        var act = () => CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock, competitionStages: stages);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawGroupSeatsReservedByInboundFeed);
        stage.Draws.Should().BeEmpty();
    }

    [Fact]
    public void Occupant_outside_the_composition_is_refused()
    {
        var (_, stage, ids) = OpenGroups();
        var outsider = EntryId.New();
        stage.AssignEntryToGroup(stage.Groups[0].Id, outsider);

        var decision = GroupDrawLaunchRules.Evaluate(stage, [stage]);

        decision.Code.Should().Be(ApplicationErrorCodes.DrawFixedOutsideComposition);
        decision.Reasons.Should().Contain($"group:{stage.Groups[0].Id.Value:D}:entry:{outsider.Value:D}");
        ids.Should().NotContain(outsider);
    }

    [Fact]
    public void Occupants_above_draw_capacity_are_refused()
    {
        var competition = CreateCompetition.Execute("Capacity", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 4), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random, potRules: new PotRules(4)), _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[1]);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[2]);

        var decision = GroupDrawLaunchRules.Evaluate(stage, [stage]);

        decision.Code.Should().Be(ApplicationErrorCodes.DrawFixedExceedsCapacity);
        decision.Reasons.Should().ContainSingle().Which.Should().Contain("occupants:3:capacity:2");
    }

    [Fact]
    public void Missing_pots_are_refused()
    {
        var competition = CreateCompetition.Execute("No pots", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 2), _clock).Stage;
        stage.ReplaceAffectationAuthoring(ids, _clock);

        var decision = GroupDrawLaunchRules.Evaluate(stage, [stage]);

        decision.Code.Should().Be(ApplicationErrorCodes.DrawPotsMissing);
    }

    [Fact]
    public void Undivisible_shape_is_refused()
    {
        var competition = CreateCompetition.Execute("Shape", _clock);
        var ids = Enumerable.Range(0, 3)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 2), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random, potRules: new PotRules(2)), _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);

        var decision = GroupDrawLaunchRules.Evaluate(stage, [stage]);

        decision.Code.Should().Be(ApplicationErrorCodes.DrawGroupShapeInvalid);
        var hub = StructureViewAssembler.Assemble(competition, [stage]).Stages.Single();
        hub.DrawCreateLaunch.BlockReasonCode.Should().Be(StructureDrawCreateLaunchGate.BlockGroupShapeInvalid);
    }

    [Fact]
    public void Rerun_on_an_occupied_grid_stays_vacant_grid()
    {
        var (_, stage, ids) = OpenGroups();
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock, competitionStages: [stage]);

        var act = () => ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group, DrawInputsIntent.Rerun),
            DrawInputsIntent.Rerun,
            [stage]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawLaunchGridNotVacant);
    }

    [Fact]
    public void Default_inputs_on_a_legal_partial_grid_are_accepted()
    {
        var (_, stage, ids) = OpenGroups();
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock, competitionStages: [stage]);

        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group),
            DrawInputsIntent.Default,
            [stage]);

        stage.GetDraw(draw.Id).Inputs!.FixedGroups.Should().ContainSingle()
            .Which.EntryId.Should().Be(ids[0]);
    }

    private (Competition Competition, Stage Stage, EntryId[] Ids) OpenGroups()
    {
        var competition = CreateCompetition.Execute("Groups", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 2), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random, potRules: new PotRules(2)), _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        return (competition, stage, ids);
    }
}
