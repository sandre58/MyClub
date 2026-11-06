// -----------------------------------------------------------------------
// <copyright file="GroupPlacementGOps3B1Tests.cs" company="Stéphane ANDRE">
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
public sealed class GroupPlacementGOps3B1Tests
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

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.GroupAssignmentEntryNotInComposition);
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
        move.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.GroupManualEditResolutionLocked);

        var clear = () => ClearEntryFromGroup.Execute(
            stage,
            competition,
            groupA.Id,
            entryId);
        clear.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.GroupManualEditResolutionLocked);
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
