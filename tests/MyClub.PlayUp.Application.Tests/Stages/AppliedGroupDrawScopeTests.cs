// -----------------------------------------------------------------------
// <copyright file="AppliedGroupDrawScopeTests.cs" company="Stéphane ANDRE">
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
/// Applied group draws protect and count only teams the draw placed.
/// </summary>
public sealed class AppliedGroupDrawScopeTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 21, 12, 0, 0, TimeSpan.Zero));

    [Theory]
    [InlineData("manual")]
    [InlineData("qual")]
    [InlineData("drawn")]
    public void Seat_rules_follow_the_drawn_scope(string kind)
    {
        var world = AppliedWorld();
        var subject = kind switch
        {
            "manual" => world.Manual,
            "qual" => world.Qualified,
            _ => world.Drawn
        };
        var otherGroup = world.Stage.Groups.First(group => !group.Id.Equals(subject.GroupId)).Id;
        var assignTarget = kind == "manual" ? subject.GroupId : otherGroup;
        var assign = ManualGroupSeatRules.EvaluateAssign(
            world.Stage,
            [world.Stage],
            world.Stage.GetGroup(assignTarget),
            subject.EntryId,
            null);
        var clear = ManualGroupSeatRules.EvaluateClear(world.Stage, subject.GroupId, subject.EntryId);
        var replace = ManualGroupSeatRules.EvaluateReplace(
            world.Stage,
            subject.GroupId,
            subject.EntryId,
            subject.EntryId);
        var seat = world.Stage.GetGroup(subject.GroupId);
        var index = seat.EntryIds.ToList().FindIndex(id => id.Equals(subject.EntryId)) + 1;
        var projected = ManualGroupSeatRules.Project(
            world.Stage,
            [world.Stage],
            seat,
            index,
            subject.EntryId,
            [.. world.Stage.CompositionEntries.Select(entry => entry.EntryId)],
            [],
            0);

        switch (kind)
        {
            case "manual":
                assign.Should().BeNull();
                ManualGroupSeatRules.EvaluateAssign(
                        world.Stage,
                        [world.Stage],
                        world.Stage.GetGroup(otherGroup),
                        subject.EntryId,
                        null)
                    .Should().Be(StageErrorCodes.DuplicateEntry);
                clear.Should().BeNull();
                var spare = AddEntry.Execute(world.Competition, "Spare", _clock).Id;
                var population = world.Stage.CompositionEntries
                    .Select(entry => entry.EntryId)
                    .Append(spare)
                    .ToArray();
                world.Stage.ReplaceAffectationAuthoring(population, _clock);
                ManualGroupSeatRules.EvaluateReplace(
                        world.Stage,
                        subject.GroupId,
                        subject.EntryId,
                        spare)
                    .Should().BeNull();
                var drawnElsewhere = world.Stage.GetDraw(world.DrawId).Resolution.GroupResults
                    .First(row =>
                        !row.GroupId.Equals(subject.GroupId)
                        && !row.EntryId.Equals(subject.EntryId)
                        && !row.EntryId.Equals(world.Qualified.EntryId));
                ManualGroupSeatRules.EvaluateReplace(
                        world.Stage,
                        subject.GroupId,
                        subject.EntryId,
                        drawnElsewhere.EntryId)
                    .Should().Be(ApplicationErrorCodes.AppliedDrawGroupPlacementProtected);
                projected.SeatMode.Should().Be(GroupSeatModes.Editable);
                projected.CanClear.Should().BeTrue();
                break;
            case "qual":
                assign.Should().Be(StageErrorCodes.GroupPlacementResolutionLocked);
                clear.Should().Be(StageErrorCodes.GroupPlacementResolutionLocked);
                replace.Should().Be(StageErrorCodes.GroupPlacementResolutionLocked);
                projected.SeatMode.Should().Be(GroupSeatModes.QualProg);
                projected.CanClear.Should().BeFalse();
                break;
            default:
                assign.Should().Be(ApplicationErrorCodes.AppliedDrawGroupPlacementProtected);
                clear.Should().Be(ApplicationErrorCodes.AppliedDrawGroupPlacementProtected);
                replace.Should().Be(ApplicationErrorCodes.AppliedDrawGroupPlacementProtected);
                projected.SeatMode.Should().Be(GroupSeatModes.Draw);
                projected.CanClear.Should().BeFalse();
                break;
        }
    }

    [Fact]
    public void Null_inputs_keep_the_whole_resolution_protected()
    {
        var (competition, stage, draw) = ApplyOnEmptyGrid();
        var applied = stage.GetDraw(draw.Id);
        typeof(Draw).GetProperty(nameof(Draw.Inputs))!.SetValue(applied, null);

        DrawAppliedState.IsApplied(applied, stage).Should().BeTrue();
        AppliedGroupDrawScope.DrawnPlacements(applied).Select(row => row.EntryId)
            .Should().BeEquivalentTo(applied.Resolution.GroupResults.Select(row => row.EntryId));
        foreach (var placement in applied.Resolution.GroupResults)
        {
            var clear = () => ClearEntryFromGroup.Execute(
                stage,
                competition,
                placement.GroupId,
                placement.EntryId);
            clear.Should().Throw<ApplicationFailureException>()
                .Which.Code.Should().Be(ApplicationErrorCodes.AppliedDrawGroupPlacementProtected);
        }
    }

    [Fact]
    public void Rerun_intent_on_an_empty_grid_still_protects_every_occupant()
    {
        var (competition, stage, draw) = ApplyOnEmptyGrid(DrawInputsIntent.Rerun);
        var applied = stage.GetDraw(draw.Id);
        applied.Inputs!.FixedGroups.Should().BeEmpty();
        DrawAppliedState.IsApplied(applied, stage).Should().BeTrue();
        var placement = applied.Resolution.GroupResults[0];
        var clear = () => ClearEntryFromGroup.Execute(
            stage,
            competition,
            placement.GroupId,
            placement.EntryId);
        clear.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AppliedDrawGroupPlacementProtected);
    }

    [Fact]
    public void Draw_with_no_drawn_entry_stays_applied_only_while_every_fixed_team_stays()
    {
        var (competition, stage) = FullGrid();
        var draw = PublishAndApply(stage, DrawInputsIntent.Default);
        var applied = stage.GetDraw(draw.Id);
        GroupDrawLaunchRules.DrawnEntries(applied).Should().BeEmpty();
        DrawAppliedState.IsApplied(applied, stage).Should().BeTrue();

        var first = applied.Resolution.GroupResults[0];
        ClearEntryFromGroup.Execute(stage, competition, first.GroupId, first.EntryId);

        DrawAppliedState.IsApplied(stage.GetDraw(draw.Id), stage).Should().BeFalse();
    }

    [Fact]
    public void Removing_a_fixed_team_keeps_the_draw_applied_and_its_locks()
    {
        var world = AppliedWorld();
        ClearEntryFromGroup.Execute(world.Stage, world.Competition, world.Manual.GroupId, world.Manual.EntryId);

        DrawAppliedState.IsApplied(world.Stage.GetDraw(world.DrawId), world.Stage).Should().BeTrue();
        var pool = () => StructureAppliedDrawPoolChangeGate.EnsurePoolChangeAllowed(
            world.Stage,
            [world.Manual.EntryId]);
        pool.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AppliedDrawPoolChangeBlocked);
        var rules = () => StructureAppliedDrawFeedRulesChangeGate.EnsureQualificationReplaceAllowed(
            world.Stage,
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForGroup(world.Stage.Id, world.Stage.Groups[0].Id))
            ]));
        rules.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AppliedDrawFeedRulesChangeBlocked);
        ManualGroupSeatRules.EvaluateAssign(
                world.Stage,
                [world.Stage],
                world.Stage.GetGroup(world.Manual.GroupId),
                world.Manual.EntryId,
                null)
            .Should().NotBe(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);
    }

    [Fact]
    public void Redo_after_removing_a_fixed_team_draws_that_team_and_keeps_the_other_fixes()
    {
        var world = AppliedWorld();
        ClearEntryFromGroup.Execute(world.Stage, world.Competition, world.Manual.GroupId, world.Manual.EntryId);
        var result = RedoDraw.Execute(world.Stage, world.DrawId, _clock, [world.Stage]);
        var draft = world.Stage.GetDraw(result.DrawId);
        draft.Status.Should().Be(DrawStatus.Draft);
        draft.Inputs!.FixedGroups.Select(row => row.EntryId).Should().NotContain(world.Manual.EntryId);
        draft.Inputs.FixedGroups.Select(row => row.EntryId).Should().Contain(world.Qualified.EntryId);
        draft.Resolution.GroupResults.Select(row => row.EntryId).Should().Contain(world.Manual.EntryId);
        GroupDrawLaunchRules.DrawnEntries(draft).Should().Contain(world.Manual.EntryId);
        draft.Inputs.PotMembership.Should().NotBeNull();
    }

    [Fact]
    public void Redo_blocks_when_fixed_teams_share_a_pot_in_one_group()
    {
        var competition = CreateCompetition.Execute("Pot", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"P{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 3), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random, potRules: new PotRules(2)), _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        var pot1 = GroupDrawPots.TryRead(stage)!.Where(row => row.Value == 1).Select(row => row.Key).ToArray();
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[0].Id, pot1[0]);
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[1].Id, pot1[1]);
        var draw = PublishAndApply(stage, DrawInputsIntent.Default);
        ClearEntryFromGroup.Execute(stage, competition, stage.Groups[0].Id, pot1[0]);
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[1].Id, pot1[0]);

        var redo = () => RedoDraw.Execute(stage, draw.Id, _clock, [stage]);
        redo.Should().Throw<ApplicationFailureException>()
            .Which.Should().Match<ApplicationFailureException>(error =>
                error.Code == ApplicationErrorCodes.DrawFixedPotConflict && error.Reasons.Count > 0);
    }

    [Fact]
    public void Slot_default_with_fixed_slots_keeps_protection_and_applied_state()
    {
        var competition = CreateCompetition.Execute("Slot-fixed", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"S{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(4), _clock).Stage;
        stage.ReplaceAffectationAuthoring(ids, _clock);
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        var first = PublishAndApplySlot(stage);
        CancelDraw.Execute(stage, first.Id, _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot));
        stage.GetDraw(draw.Id).Inputs!.FixedSlots.Should().NotBeEmpty();
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            slotTargets: [.. stage.Slots.Select(slot => slot.SlotKey)]).IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);

        var applied = stage.GetDraw(draw.Id);
        DrawAppliedState.IsApplied(applied, stage).Should().BeTrue();
        var slotKey = applied.Inputs!.FixedSlots[0].SlotKey;
        var clear = () => StructureAppliedDrawSlotPlacementGate.EnsureClearAllowed(stage, slotKey);
        clear.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AppliedDrawSlotPlacementProtected);
    }

    private (Competition Competition, Stage Stage, Draw Draw) ApplyOnEmptyGrid(
        DrawInputsIntent intent = DrawInputsIntent.Default)
    {
        var competition = CreateCompetition.Execute("Empty", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 2), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random, potRules: new PotRules(2)), _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        var draw = PublishAndApply(stage, intent);
        return (competition, stage, draw);
    }

    private (Competition Competition, Stage Stage) FullGrid()
    {
        var competition = CreateCompetition.Execute("Full", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"F{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 2), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random, potRules: new PotRules(2)), _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        var pots = GroupDrawPots.TryRead(stage)!;
        var byPot = pots.GroupBy(row => row.Value).ToDictionary(group => group.Key, group => group.Select(row => row.Key).ToArray());
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[0].Id, byPot[1][0]);
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[0].Id, byPot[2][0]);
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[1].Id, byPot[1][1]);
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[1].Id, byPot[2][1]);
        return (competition, stage);
    }

    private AppliedFixture AppliedWorld()
    {
        var competition = CreateCompetition.Execute("Scope", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"W{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 3), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random, potRules: new PotRules(2)), _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        var pots = GroupDrawPots.TryRead(stage)!;
        var pot1 = pots.First(row => row.Value == 1).Key;
        var pot2 = pots.First(row => row.Value == 2).Key;
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[0].Id, pot1);
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[1].Id, pot2);
        stage.WriteGroupResolutionProvenance(
            stage.Groups[1].Id,
            pot2,
            new ResolutionProvenance(ResolutionProvenanceKind.Qualification, "Q|path", "fp"));
        var draw = PublishAndApply(stage, DrawInputsIntent.Default);
        var applied = stage.GetDraw(draw.Id);
        var drawn = applied.Resolution.GroupResults.First(row =>
            !row.EntryId.Equals(pot1) && !row.EntryId.Equals(pot2));
        return new AppliedFixture(
            competition,
            stage,
            draw.Id,
            new Seat(pot1, stage.Groups[0].Id),
            new Seat(pot2, stage.Groups[1].Id),
            new Seat(drawn.EntryId, drawn.GroupId));
    }

    private Draw PublishAndApply(Stage stage, DrawInputsIntent intent)
    {
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        ConfigureDrawInputs.Execute(stage, draw.Id, DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group, intent));
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [.. stage.Groups.Select(group => group.Id)]).IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        return stage.GetDraw(draw.Id);
    }

    private Draw PublishAndApplySlot(Stage stage)
    {
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(stage, draw.Id, DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot));
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            slotTargets: [.. stage.Slots.Select(slot => slot.SlotKey)]).IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        return stage.GetDraw(draw.Id);
    }

    private sealed record Seat(EntryId EntryId, GroupId GroupId);

    private sealed record AppliedFixture(
        Competition Competition,
        Stage Stage,
        DrawId DrawId,
        Seat Manual,
        Seat Qualified,
        Seat Drawn);
}
