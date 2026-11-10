// -----------------------------------------------------------------------
// <copyright file="ManualGroupPlacementTests.cs" company="Stéphane ANDRE">
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
/// Manual group assign/clear versus an Applied group draw and Qual/Prog resolution locks.
/// </summary>
public sealed class ManualGroupPlacementTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Clear_refuses_when_membership_protected_by_applied_group_draw()
    {
        var (competition, stage, draw, _) = SeedGroupsWithAppliedDraw();
        var placement = draw.Resolution.GroupResults[0];

        var act = () => ClearEntryFromGroup.Execute(
            stage,
            competition,
            placement.GroupId,
            placement.EntryId);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AppliedDrawGroupPlacementProtected);
    }

    [Fact]
    public void Assign_refuses_when_changing_protected_entry_to_another_group()
    {
        var (competition, stage, draw, groupIds) = SeedGroupsWithAppliedDraw();
        var placement = draw.Resolution.GroupResults[0];
        var otherGroup = groupIds.First(id => !id.Equals(placement.GroupId));

        var act = () => AssignEntryToGroup.Execute(
            stage,
            competition,
            otherGroup,
            placement.EntryId);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AppliedDrawGroupPlacementProtected);
    }

    [Fact]
    public void Assign_allows_same_entry_on_protected_group()
    {
        var (competition, stage, draw, _) = SeedGroupsWithAppliedDraw();
        var placement = draw.Resolution.GroupResults[0];

        AssignEntryToGroup.Execute(
            stage,
            competition,
            placement.GroupId,
            placement.EntryId);

        stage.GetGroup(placement.GroupId).EntryIds.Should().Contain(placement.EntryId);
    }

    [Fact]
    public void Assign_refuses_entry_not_in_composition_pool()
    {
        var competition = CreateCompetition.Execute("G-not-pool", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(_ => AddEntry.Execute(competition, $"T{_}", _clock).Id)
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
        var outsider = AddEntry.Execute(competition, "Outside", _clock).Id;

        var act = () => AssignEntryToGroup.Execute(
            stage,
            competition,
            stage.Groups[0].Id,
            outsider);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.GroupAssignmentEntryNotInComposition);
    }

    [Fact]
    public void Assign_and_clear_refuse_when_qualification_resolution_locks_membership()
    {
        var (competition, stage, groupA, entryId) = SeedDraftGroupsWithManualPlacement();
        var qualProvenance = new ResolutionProvenance(
            ResolutionProvenanceKind.Qualification,
            "Q|stub-path",
            "stub-fp");
        stage.WriteGroupResolutionProvenance(groupA.Id, entryId, qualProvenance);

        var otherGroup = stage.Groups.First(g => !g.Id.Equals(groupA.Id));

        var move = () => AssignEntryToGroup.Execute(
            stage,
            competition,
            otherGroup.Id,
            entryId);
        move.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.GroupPlacementResolutionLocked);

        var clear = () => ClearEntryFromGroup.Execute(
            stage,
            competition,
            groupA.Id,
            entryId);
        clear.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.GroupPlacementResolutionLocked);
    }

    [Fact]
    public void Assign_in_Ready_without_a_match_is_allowed()
    {
        var (competition, stage, entryId) = SeedReadyGroups();

        AssignEntryToGroup.Execute(stage, competition, stage.Groups[0].Id, entryId);

        stage.Groups[0].EntryIds.Should().Contain(entryId);
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void Placement_progress_caps_each_group_and_blocks_when_a_group_is_not_exact()
    {
        var uneven = GroupsWithMembership([5, 3]);
        var (filled, capacity, blocks) = GroupsPlacementReadiness.Evaluate(uneven);
        blocks.Should().BeTrue();
        filled.Should().Be(7);
        capacity.Should().Be(8);

        var even = GroupsWithMembership([4, 4]);
        var ready = GroupsPlacementReadiness.Evaluate(even);
        ready.BlocksProduction.Should().BeFalse();
        ready.Filled.Should().Be(8);
        ready.Capacity.Should().Be(8);
    }

    [Fact]
    public void Groups_stage_exposes_manual_placement_actions_in_Draft_and_Ready_only()
    {
        var competition = CreateCompetition.Execute("Actions", _clock);
        var groups = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock).Stage;
        var draft = StructureViewAssembler.Assemble(competition, [groups]).Stages.Single();
        draft.Actions.Should().Contain(StructureViewAssembler.ActionAssignEntryToGroup);
        draft.Actions.Should().Contain(StructureViewAssembler.ActionClearEntryFromGroup);

        var ids = Enumerable.Range(0, 4)
            .Select(_ => AddEntry.Execute(competition, $"T{_}", _clock).Id)
            .ToArray();
        groups.ReplaceAffectationAuthoring(ids, _clock);
        groups.Prepare(_clock);
        var ready = StructureViewAssembler.Assemble(competition, [groups]).Stages.Single();
        ready.Actions.Should().Contain(StructureViewAssembler.ActionAssignEntryToGroup);
        ready.Actions.Should().Contain(StructureViewAssembler.ActionClearEntryFromGroup);

        var championship = CreateCompetition.Execute("Champ", _clock);
        var champStage = ConfigureStructure.Execute(
            championship,
            null,
            StructureIntent.Championship(),
            _clock).Stage;
        var champ = StructureViewAssembler.Assemble(championship, [champStage]).Stages.Single();
        champ.Actions.Should().NotContain(StructureViewAssembler.ActionAssignEntryToGroup);
        champ.Actions.Should().NotContain(StructureViewAssembler.ActionClearEntryFromGroup);

        var cupCompetition = CreateCompetition.Execute("Cup", _clock);
        var cupStage = ConfigureStructure.Execute(
            cupCompetition,
            null,
            StructureIntent.Cup(4),
            _clock).Stage;
        var cup = StructureViewAssembler.Assemble(cupCompetition, [cupStage]).Stages.Single();
        cup.Actions.Should().NotContain(StructureViewAssembler.ActionAssignEntryToGroup);
        cup.Actions.Should().NotContain(StructureViewAssembler.ActionClearEntryFromGroup);
    }

    private (Competition Competition, Stage Stage, EntryId EntryId) SeedReadyGroups()
    {
        var competition = CreateCompetition.Execute("G-ready", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(i => AddEntry.Execute(competition, $"T{i}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock).Stage;
        stage.ReplaceAffectationAuthoring(ids, _clock);
        stage.Prepare(_clock);
        return (competition, stage, ids[0]);
    }

    private Stage GroupsWithMembership(int[] counts)
    {
        var competition = CreateCompetition.Execute($"G-{string.Join('-', counts)}", _clock);
        var total = counts.Sum();
        var ids = Enumerable.Range(0, total)
            .Select(i => AddEntry.Execute(competition, $"T{i}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(counts.Length, 4),
            _clock).Stage;
        stage.SetPlacesPerGroup(null);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        var cursor = 0;
        for (var groupIndex = 0; groupIndex < counts.Length; groupIndex++)
        {
            for (var member = 0; member < counts[groupIndex]; member++)
            {
                stage.AssignEntryToGroup(stage.Groups[groupIndex].Id, ids[cursor]);
                cursor++;
            }
        }

        stage.SetPlacesPerGroup(4);
        return stage;
    }

    private (Competition Competition, Stage Stage, Draw Draw, GroupId[] GroupIds) SeedGroupsWithAppliedDraw()
    {
        var competition = CreateCompetition.Execute("G-OPS3-B1", _clock);
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
        ApplyDraw.Execute(stage, draw.Id, _clock);
        DrawAppliedState.IsApplied(draw, stage).Should().BeTrue();

        return (competition, stage, stage.GetDraw(draw.Id), stage.Groups.Select(g => g.Id).ToArray());
    }

    private (Competition Competition, Stage Stage, Group GroupA, EntryId EntryId) SeedDraftGroupsWithManualPlacement()
    {
        var competition = CreateCompetition.Execute("G-qual-lock", _clock);
        var entryId = AddEntry.Execute(competition, "Locked", _clock).Id;
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock).Stage;
        stage.ReplaceAffectationAuthoring([entryId], _clock);
        var groupA = stage.Groups[0];
        AssignEntryToGroup.Execute(stage, competition, groupA.Id, entryId);
        return (competition, stage, groupA, entryId);
    }
}
