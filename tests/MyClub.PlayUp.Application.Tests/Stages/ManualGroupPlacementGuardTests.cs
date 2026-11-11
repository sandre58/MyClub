// -----------------------------------------------------------------------
// <copyright file="ManualGroupPlacementGuardTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// Manual group placement stays closed for reserved feeds and for an unapplied group draw,
/// and qualification apply does not use that manual guard.
/// </summary>
public sealed class ManualGroupPlacementGuardTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 11, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assign_is_allowed_when_free_places_exceed_pending_feeds()
    {
        var competition = CreateCompetition.Execute("Free", _clock);
        var entryId = AddEntry.Execute(competition, "Manual", _clock).Id;
        var source = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2), _clock);
        var destination = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 4), _clock);
        destination.ReplaceAffectationAuthoring([entryId], _clock);
        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(source.Groups[0].Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForGroup(destination.Id, destination.Groups[0].Id))
            ]),
            _clock);

        var stages = new[] { source, destination };
        var seat = Project(destination, competition, stages, destination.Groups[0].Id, index: 1);
        seat.SeatMode.Should().Be(StageSchematicAssembler.GroupSeatEditable);
        seat.FeedOrigin.Should().NotBeNull();

        AssignEntryToGroup.Execute(
            destination,
            competition,
            destination.Groups[0].Id,
            entryId,
            competitionStages: stages);
        destination.Groups[0].EntryIds.Should().Contain(entryId);
    }

    [Fact]
    public void Qualification_apply_still_fills_a_group_that_manual_placement_treats_as_reserved()
    {
        var competition = CreateCompetition.Execute("Qual", _clock);
        var manualId = AddEntry.Execute(competition, "Manual", _clock).Id;
        var sourceIds = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"S{index}", _clock).Id)
            .ToArray();
        var source = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2), _clock);
        var destination = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2), _clock);
        destination.ReplaceAffectationAuthoring([manualId], _clock);
        source.AssignEntryToGroup(source.Groups[0].Id, sourceIds[0]);
        source.AssignEntryToGroup(source.Groups[0].Id, sourceIds[1]);
        source.AssignEntryToGroup(source.Groups[1].Id, sourceIds[2]);
        source.AssignEntryToGroup(source.Groups[1].Id, sourceIds[3]);
        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(source.Groups[0].Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForGroup(destination.Id, destination.Groups[0].Id)),
                new QualificationPath(
                    2,
                    QualificationSource.FromGroup(source.Groups[1].Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForGroup(destination.Id, destination.Groups[0].Id))
            ]),
            _clock);

        var stages = new[] { source, destination };
        var reserved = () => AssignEntryToGroup.Execute(
            destination,
            competition,
            destination.Groups[0].Id,
            manualId,
            competitionStages: stages);
        reserved.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.GroupSeatReservedByInboundFeed);

        var matches = new List<Match>
        {
            Finish(Match.Create(competition.Id, source.Id, sourceIds[0], sourceIds[1], _clock), 2, 0),
            Finish(Match.Create(competition.Id, source.Id, sourceIds[2], sourceIds[3], _clock), 1, 0)
        };
        var standings = new Dictionary<GroupId, Standing>
        {
            [source.Groups[0].Id] = CalculateStanding.Execute(
                source.Groups[0].EntryIds,
                matches,
                source.Regulation.StandingRules.OrThrow()),
            [source.Groups[1].Id] = CalculateStanding.Execute(
                source.Groups[1].EntryIds,
                matches,
                source.Regulation.StandingRules.OrThrow())
        };

        ApplyQualification.Execute(source, overallStanding: null, standings, stages, _clock);

        destination.Groups[0].EntryIds.Should().Contain(sourceIds[0]);
        destination.Groups[0].EntryIds.Should().Contain(sourceIds[2]);
        destination.Groups[0].EntryIds.Should().NotContain(manualId);
    }

    [Fact]
    public void Assign_and_clear_are_refused_while_a_group_draw_is_in_progress_and_allowed_after_cancel()
    {
        var (competition, stage, placed, other) = GroupStageWithDrawRules();
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[0].Id, placed);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);

        var assign = () => AssignEntryToGroup.Execute(stage, competition, stage.Groups[1].Id, other);
        var clear = () => ClearEntryFromGroup.Execute(stage, competition, stage.Groups[0].Id, placed);
        assign.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);
        clear.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);

        CancelDraw.Execute(stage, draw.Id, _clock);
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[1].Id, other);
        ClearEntryFromGroup.Execute(stage, competition, stage.Groups[0].Id, placed);
        stage.Groups[1].EntryIds.Should().Contain(other);
        stage.Groups[0].EntryIds.Should().NotContain(placed);
    }

    [Fact]
    public void Assign_and_clear_are_refused_for_a_no_solution_draft_and_follow_draw_protection_after_apply()
    {
        var (competition, stage, placed, other) = GroupStageWithDrawRules();
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[0].Id, placed);
        var orphan = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        stage.MarkDrawNoSolution(orphan.Id, _clock);
        stage.GetDraw(orphan.Id).Status.Should().Be(DrawStatus.Draft);

        var duringOrphan = () => AssignEntryToGroup.Execute(stage, competition, stage.Groups[1].Id, other);
        duringOrphan.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);
        CancelDraw.Execute(stage, orphan.Id, _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group));
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [.. stage.Groups.Select(group => group.Id)]).IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);

        var beforeApply = () => ClearEntryFromGroup.Execute(stage, competition, stage.Groups[0].Id, placed);
        beforeApply.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);

        ApplyDraw.Execute(stage, draw.Id, _clock);
        var placement = stage.GetDraw(draw.Id).Resolution.GroupResults
            .First(result => result.EntryId.Equals(placed));
        AssignEntryToGroup.Execute(stage, competition, placement.GroupId, placed);
        var clearDrawn = () => ClearEntryFromGroup.Execute(
            stage,
            competition,
            placement.GroupId,
            placed);
        clearDrawn.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AppliedDrawGroupPlacementProtected);
    }

    [Fact]
    public void After_apply_an_undrawn_entry_can_be_placed_and_cleared_in_a_group_that_is_not_full()
    {
        var (competition, stage) = DrawnGroups(places: 4, drawnEntries: 4);
        var extra = AddEntry.Execute(competition, "Extra", _clock).Id;
        stage.ReplaceAffectationAuthoring(
            [.. stage.CompositionEntries.Select(entry => entry.EntryId), extra],
            _clock);

        AssignEntryToGroup.Execute(stage, competition, stage.Groups[0].Id, extra);
        stage.Groups[0].EntryIds.Should().Contain(extra);
        ClearEntryFromGroup.Execute(stage, competition, stage.Groups[0].Id, extra);
        stage.Groups[0].EntryIds.Should().NotContain(extra);
    }

    [Fact]
    public void Resolved_qualification_frees_the_place_and_one_of_two_paths_uses_the_remaining_count()
    {
        var competition = CreateCompetition.Execute("Resolved", _clock);
        var manualId = AddEntry.Execute(competition, "Manual", _clock).Id;
        var sourceIds = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"S{index}", _clock).Id)
            .ToArray();
        var source = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2), _clock);
        var destination = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 4), _clock);
        destination.ReplaceAffectationAuthoring([manualId], _clock);
        source.AssignEntryToGroup(source.Groups[0].Id, sourceIds[0]);
        source.AssignEntryToGroup(source.Groups[0].Id, sourceIds[1]);
        source.AssignEntryToGroup(source.Groups[1].Id, sourceIds[2]);
        source.AssignEntryToGroup(source.Groups[1].Id, sourceIds[3]);
        var firstPath = new QualificationPath(
            1,
            QualificationSource.FromGroup(source.Groups[0].Id),
            new QualificationSelection(SelectionMode.Position, 1),
            QualificationDestination.ForGroup(destination.Id, destination.Groups[0].Id));
        var secondPath = new QualificationPath(
            2,
            QualificationSource.FromGroup(source.Groups[1].Id),
            new QualificationSelection(SelectionMode.Position, 1),
            QualificationDestination.ForGroup(destination.Id, destination.Groups[0].Id));
        source.ReplaceQualificationRules(new QualificationRules([firstPath, secondPath]), _clock);
        var stages = new[] { source, destination };
        var matches = new List<Match>
        {
            Finish(Match.Create(competition.Id, source.Id, sourceIds[0], sourceIds[1], _clock), 2, 0),
            Finish(Match.Create(competition.Id, source.Id, sourceIds[2], sourceIds[3], _clock), 1, 0)
        };
        var standings = new Dictionary<GroupId, Standing>
        {
            [source.Groups[0].Id] = CalculateStanding.Execute(
                source.Groups[0].EntryIds,
                matches,
                source.Regulation.StandingRules.OrThrow()),
            [source.Groups[1].Id] = CalculateStanding.Execute(
                source.Groups[1].EntryIds,
                matches,
                source.Regulation.StandingRules.OrThrow())
        };

        ApplyQualification.Execute(source, overallStanding: null, standings, stages, _clock);

        var seat = Project(destination, competition, stages, destination.Groups[0].Id, index: 3);
        seat.Entry.Should().BeNull();
        seat.SeatMode.Should().Be(GroupSeatModes.Editable);
        AssignEntryToGroup.Execute(
            destination,
            competition,
            destination.Groups[0].Id,
            manualId,
            competitionStages: stages);
        destination.Groups[0].EntryIds.Should().Contain(manualId);
    }

    [Fact]
    public void One_resolved_feed_of_two_no_longer_counts_the_resolved_path()
    {
        var competition = CreateCompetition.Execute("Partial", _clock);
        var ids = Enumerable.Range(0, 3)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var source = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2), _clock);
        var destination = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 4), _clock);
        destination.ReplaceAffectationAuthoring(ids, _clock);
        destination.AssignEntryToGroup(destination.Groups[0].Id, ids[0]);
        destination.AssignEntryToGroup(destination.Groups[0].Id, ids[1]);
        var firstPath = new QualificationPath(
            1,
            QualificationSource.FromGroup(source.Groups[0].Id),
            new QualificationSelection(SelectionMode.Position, 1),
            QualificationDestination.ForGroup(destination.Id, destination.Groups[0].Id));
        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                firstPath,
                new QualificationPath(
                    2,
                    QualificationSource.FromGroup(source.Groups[1].Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForGroup(destination.Id, destination.Groups[0].Id))
            ]),
            _clock);
        var stages = new[] { source, destination };
        var before = Project(destination, competition, stages, destination.Groups[0].Id, index: 3);
        before.SeatMode.Should().Be(GroupSeatModes.Reserved);
        var blocked = () => AssignEntryToGroup.Execute(
            destination,
            competition,
            destination.Groups[0].Id,
            ids[2],
            competitionStages: stages);
        blocked.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.GroupSeatReservedByInboundFeed);

        destination.WriteGroupResolutionProvenance(
            destination.Groups[0].Id,
            ids[0],
            new ResolutionProvenance(
                ResolutionProvenanceKind.Qualification,
                ResolutionPathIdentifier.ForQualification(source.Id, firstPath),
                "fp"));

        var after = Project(destination, competition, stages, destination.Groups[0].Id, index: 3);
        after.SeatMode.Should().Be(GroupSeatModes.Editable);
        AssignEntryToGroup.Execute(
            destination,
            competition,
            destination.Groups[0].Id,
            ids[2],
            competitionStages: stages);
        destination.Groups[0].EntryIds.Should().Contain(ids[2]);
    }

    [Fact]
    public void A_published_draw_that_no_longer_matches_the_groups_still_blocks_manual_placement()
    {
        var (competition, stage) = DrawnGroups(places: 2, drawnEntries: 4);
        var pinned = stage.Groups[0].EntryIds[0];
        var remaining = stage.CompositionEntries
            .Select(entry => entry.EntryId)
            .Where(id => !id.Equals(pinned))
            .ToArray();
        stage.ReplaceAffectationAuthoring(remaining, _clock);

        var seat = Project(stage, competition, [stage], stage.Groups[0].Id, index: 1);
        seat.SeatMode.Should().Be(GroupSeatModes.DrawPending);
        var assign = () => AssignEntryToGroup.Execute(stage, competition, stage.Groups[1].Id, remaining[0]);
        var clear = () => ClearEntryFromGroup.Execute(stage, competition, stage.Groups[1].Id, stage.Groups[1].EntryIds[0]);
        assign.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);
        clear.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);
    }

    private (Competition Competition, Stage Stage) DrawnGroups(int places, int drawnEntries)
    {
        var competition = CreateCompetition.Execute($"Drawn-{places}", _clock);
        var ids = Enumerable.Range(0, drawnEntries)
            .Select(index => AddEntry.Execute(competition, $"D{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, places),
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
            groupTargets: [.. stage.Groups.Select(group => group.Id)]).IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        return (competition, stage);
    }

    private SchematicCaseDto Project(
        Stage stage,
        Competition competition,
        IReadOnlyList<Stage> stages,
        GroupId groupId,
        int index)
    {
        _ = _clock;
        var schematic = StageSchematicAssembler.Assemble(stage, competition, stages);
        return schematic.Cases.Single(item =>
            item.FormPosition.GroupId == groupId.Value && item.FormPosition.Index == index);
    }

    private (Competition Competition, Stage Stage, EntryId Placed, EntryId Other) GroupStageWithDrawRules()
    {
        var competition = CreateCompetition.Execute("Draw-guard", _clock);
        var placed = AddEntry.Execute(competition, "Placed", _clock).Id;
        var other = AddEntry.Execute(competition, "Other", _clock).Id;
        var rest = Enumerable.Range(0, 2)
            .Select(index => AddEntry.Execute(competition, $"R{index}", _clock).Id)
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
        stage.ReplaceAffectationAuthoring([placed, other, rest[0], rest[1]], _clock);
        return (competition, stage, placed, other);
    }

    private Match Finish(Match match, int homeGoals, int awayGoals)
    {
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(homeGoals, awayGoals)), _clock);
        return match;
    }
}
