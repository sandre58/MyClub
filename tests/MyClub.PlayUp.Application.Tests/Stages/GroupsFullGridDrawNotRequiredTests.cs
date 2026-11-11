// -----------------------------------------------------------------------
// <copyright file="GroupsFullGridDrawNotRequiredTests.cs" company="Stéphane ANDRE">
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
/// A complete Groups grid does not require a draw. Cup and an incomplete Groups grid still do.
/// </summary>
public sealed class GroupsFullGridDrawNotRequiredTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 11, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Full_groups_grid_with_draw_rules_does_not_require_a_draw_and_allows_production()
    {
        var (competition, stage) = GroupsWithDrawRules(placeEveryone: true);
        StageLifecycleTestSupport.EnsureReadyForMaterialize(competition, stage, [stage], _clock);

        var view = StructureViewAssembler.Assemble(competition, [stage]);
        var hub = view.Stages.Single();
        hub.HasDrawRules.Should().BeTrue();
        hub.DrawExecutionBadge.Should().BeNull();
        view.Readiness.NextConstructionStepHint.Should().NotBe(StructureViewAssembler.NextStepRunDraw);
        hub.Materialize!.CommandEnabled.Should().BeTrue();
        hub.Materialize.DisabledReason.Should().BeNull();
        view.HubNextStep!.Code.Should().Be(hub.Materialize.CommandCode);
        view.HubNextStep.Code.Should().NotBe(StructureViewAssembler.NextStepRunDraw);
    }

    [Fact]
    public void Full_groups_grid_keeps_an_in_progress_draw_badge()
    {
        var (competition, stage) = GroupsWithDrawRules(placeEveryone: true);
        CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);

        var hub = StructureViewAssembler.Assemble(competition, [stage]).Stages.Single();
        hub.HasDrawRules.Should().BeTrue();
        hub.DrawExecutionBadge.Should().Be(StructureDrawExecutionBadge.InProgress);
    }

    [Fact]
    public void Incomplete_groups_grid_with_draw_rules_still_requires_a_draw()
    {
        var (competition, stage) = GroupsWithDrawRules(placeEveryone: false);

        var view = StructureViewAssembler.Assemble(competition, [stage]);
        var hub = view.Stages.Single();
        hub.HasDrawRules.Should().BeTrue();
        hub.DrawExecutionBadge.Should().Be(StructureDrawExecutionBadge.ToLaunch);
        view.Readiness.NextConstructionStepHint.Should().Be(StructureViewAssembler.NextStepRunDraw);
        hub.Materialize!.CommandEnabled.Should().BeFalse();
    }

    [Fact]
    public void Cup_with_draw_rules_still_requires_a_draw()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        foreach (var name in new[] { "A", "B", "C", "D" })
        {
            AddEntry.Execute(competition, name, _clock);
        }

        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(4), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring(
            [.. competition.Entries.Select(entry => entry.Id)],
            _clock);

        var view = StructureViewAssembler.Assemble(competition, [stage]);
        var hub = view.Stages.Single();
        hub.HasDrawRules.Should().BeTrue();
        hub.DrawExecutionBadge.Should().Be(StructureDrawExecutionBadge.ToLaunch);
        view.Readiness.NextConstructionStepHint.Should().Be(StructureViewAssembler.NextStepRunDraw);
        hub.Materialize!.CommandEnabled.Should().BeFalse();
    }

    [Fact]
    public void Full_groups_grid_reports_no_stale_resolution_and_a_null_badge_is_not_awaiting_a_draw()
    {
        var (competition, stage) = GroupsWithDrawRules(placeEveryone: true);
        StageLifecycleTestSupport.EnsureReadyForMaterialize(competition, stage, [stage], _clock);

        var hub = StructureViewAssembler.Assemble(competition, [stage]).Stages.Single();
        hub.DrawExecutionBadge.Should().BeNull();
        hub.Materialize!.CommandEnabled.Should().BeTrue();
        (hub.ResolutionOccupants ?? []).Should()
            .NotContain(occupant => occupant.State == StructureResolutionFreshness.Perime);
        StructurePlaceOccupancyIssues.IsSlotFeedAwaitingDraw(true, null).Should().BeFalse();
        StructurePlaceOccupancyIssues.DeferGroupsNotReadyForDraw(true, null).Should().BeFalse();
        StructurePlaceOccupancyIssues.IsSlotFeedAwaitingDraw(true, StructureDrawExecutionBadge.ToLaunch)
            .Should().BeTrue();
    }

    [Fact]
    public void Full_groups_grid_with_only_a_cancelled_draw_still_does_not_require_a_draw()
    {
        var (competition, stage) = GroupsWithDrawRules(placeEveryone: true);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        CancelDraw.Execute(stage, draw.Id, _clock);
        StageLifecycleTestSupport.EnsureReadyForMaterialize(competition, stage, [stage], _clock);

        var hub = StructureViewAssembler.Assemble(competition, [stage]).Stages.Single();
        hub.HasDrawRules.Should().BeTrue();
        hub.DrawExecutionBadge.Should().BeNull();
        hub.Materialize!.CommandEnabled.Should().BeTrue();
        (hub.ResolutionOccupants ?? []).Should()
            .NotContain(occupant => occupant.State == StructureResolutionFreshness.Perime);
    }

    private (Domain.Competitions.Competition Competition, Stage Stage) GroupsWithDrawRules(bool placeEveryone)
    {
        var competition = CreateCompetition.Execute(placeEveryone ? "Full" : "Open", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
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
        stage.ReplaceAffectationAuthoring(placeEveryone ? ids : ids.Take(2).ToArray(), _clock);
        if (placeEveryone)
        {
            stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
            stage.AssignEntryToGroup(stage.Groups[0].Id, ids[1]);
            stage.AssignEntryToGroup(stage.Groups[1].Id, ids[2]);
            stage.AssignEntryToGroup(stage.Groups[1].Id, ids[3]);
        }

        return (competition, stage);
    }
}
