// -----------------------------------------------------------------------
// <copyright file="StructureDrawCreateLaunchGateContractTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

/// <summary>
/// Hub projection of <see cref="StructureDrawCreateLaunchGate"/> (see
/// docs/design/structure-lifecycle/structure-draw-create-launch-gate.md).
/// </summary>
public sealed class StructureDrawCreateLaunchGateContractTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 18, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Precursor_ToLaunch_badge_when_DrawRules_engaged_and_no_active_draw()
    {
        var situation = TestCompetition.Create("Cup launch badge", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);

        var badge = StructureViewAssembler.GetDrawExecutionBadge(stage, hasDrawRules: true);

        badge.Should().Be(StructureDrawExecutionBadge.ToLaunch);
    }

    [Fact]
    public void Precursor_InProgress_badge_after_CreateDraw_blocks_first_launch_window()
    {
        var situation = TestCompetition.Create("Cup draft draw", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);

        var badge = StructureViewAssembler.GetDrawExecutionBadge(stage, hasDrawRules: true);

        badge.Should().Be(StructureDrawExecutionBadge.InProgress);

        // Contract: DrawCreateLaunchAllowed == false (an active non-cancelled draw exists).
    }

    [Fact]
    public void Precursor_slot_grid_with_blocking_places_forbids_launch_even_when_badge_ToLaunch()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var e1 = AddEntry.Execute(competition, "A", _clock);
        var e2 = AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(2), _clock);
        var stage = configured.Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring([e1.Id, e2.Id], _clock);
        stage.ApplyResolvedEntry(stage.Slots[0].SlotKey, e1.Id, _clock);

        var facts = StageDrawBracketLaunchClassifier.ClassifyForDrawKind(
            stage,
            [stage],
            DrawResolutionKind.Slot);

        facts.BlockingPlaceCount.Should().BeGreaterThan(0);
        StructureViewAssembler.GetDrawExecutionBadge(stage, hasDrawRules: true)
            .Should().Be(StructureDrawExecutionBadge.ToLaunch);

        var hub = StructureViewAssembler.Assemble(competition, [stage]).Stages.Single();
        hub.DrawCreateLaunch.Allowed.Should().BeFalse();
        hub.DrawCreateLaunch.BlockReasonCode.Should().Be(StructureDrawCreateLaunchGate.BlockBracketPlacesOccupied);
    }

    [Fact]
    public void Precursor_slot_vacant_grid_with_full_pool_and_no_direct_assignment_allows_launch_layer_L6()
    {
        var situation = TestCompetition.Create("Cup vacant grid", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring(
            [.. situation.Competition.Entries.Select(entry => entry.Id)],
            _clock);

        stage.DirectAssignments.Should().BeEmpty();
        var facts = StageDrawBracketLaunchClassifier.ClassifyForDrawKind(
            stage,
            [stage],
            DrawResolutionKind.Slot);
        facts.BlockingPlaceCount.Should().Be(0);

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages).Stages.Single();
        hub.CompositionEntryCount.Should().Be(4);
        hub.CompositionCapacity.Should().Be(4);
        hub.DrawExecutionBadge.Should().Be(StructureDrawExecutionBadge.ToLaunch);
        hub.DrawCreateLaunch.Allowed.Should().BeTrue();
        hub.DrawCreateLaunch.BlockReasonCode.Should().BeNull();
    }

    [Fact]
    public void Precursor_groups_shape_requires_pots_and_pool_product_on_hub()
    {
        var situation = TestCompetition.Create("Groups draw shape", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Groups(2, 2));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.ReplaceAffectationAuthoring(
            [.. situation.Competition.Entries.Select(entry => entry.Id)],
            _clock);

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages).Stages.Single();
        hub.NumberOfPots.Should().Be(2);
        hub.GroupCount.Should().Be(2);
        hub.CompositionEntryCount.Should().Be(4);
        hub.CompositionEntryCount.Should().Be(hub.NumberOfPots * hub.GroupCount);

        var facts = StageDrawBracketLaunchClassifier.ClassifyForDrawKind(
            stage,
            [stage],
            DrawResolutionKind.Group);
        facts.BlockingPlaceCount.Should().Be(0);
    }

    [Fact]
    public void Precursor_championship_without_draw_rules_has_no_execution_badge()
    {
        var situation = TestCompetition.Create("Champ no draw", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "League"));

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages).Stages.Single();
        hub.HasDrawRules.Should().BeFalse();
        hub.DrawExecutionBadge.Should().BeNull();
        hub.DrawCreateLaunch.Allowed.Should().BeFalse();
        hub.DrawCreateLaunch.BlockReasonCode.Should().Be(StructureDrawCreateLaunchGate.BlockDrawRulesMissing);
    }

    [Fact]
    public void Hub_exposes_group_shape_block_when_pool_does_not_match_pots_times_groups()
    {
        var situation = TestCompetition.Create("Groups shape block", _clock)
            .WithTeams("A", "B", "C")
            .WithStructure(StructureIntent.Groups(2, 2));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.ReplaceAffectationAuthoring(
            [.. situation.Competition.Entries.Select(entry => entry.Id)],
            _clock);

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages).Stages.Single();

        hub.DrawCreateLaunch.Allowed.Should().BeFalse();
        hub.DrawCreateLaunch.BlockReasonCode.Should().Be(StructureDrawCreateLaunchGate.BlockGroupShapeInvalid);
    }
}
