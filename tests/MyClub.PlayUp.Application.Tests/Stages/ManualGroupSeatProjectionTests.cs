// -----------------------------------------------------------------------
// <copyright file="ManualGroupSeatProjectionTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// Groups seat mode matches the manual assign and clear routes, including excess and outside-composition seats.
/// </summary>
public sealed class ManualGroupSeatProjectionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 11, 8, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Empty_editable_seat_accepts_assign_and_cup_and_roster_omit_the_mode()
    {
        var (competition, stage, entryId) = Groups(places: 2, entryCount: 1);

        var seat = Seat(stage, competition, stage.Groups[0].Id, index: 1);
        seat.SeatMode.Should().Be(StageSchematicAssembler.GroupSeatEditable);
        seat.CanClear.Should().BeFalse();
        seat.Excess.Should().BeFalse();
        seat.OutsideComposition.Should().BeFalse();

        AssignEntryToGroup.Execute(stage, competition, stage.Groups[0].Id, entryId);
        stage.Groups[0].EntryIds.Should().Contain(entryId);
    }

    [Fact]
    public void Cup_and_roster_cases_omit_group_seat_fields()
    {
        var cupCompetition = CreateCompetition.Execute("Cup", _clock);
        var cup = ConfigureStructure.Execute(cupCompetition, null, StructureIntent.Cup(4), _clock).Stage;
        var cupSchematic = StageSchematicAssembler.Assemble(cup, cupCompetition, [cup]);
        cupSchematic.Cases.Should().NotBeEmpty();
        cupSchematic.Cases.Should().OnlyContain(item =>
            item.SeatMode == null
            && item.CanClear == null
            && item.Excess == null
            && item.OutsideComposition == null);

        var rosterCompetition = CreateCompetition.Execute("Roster", _clock);
        AddEntry.Execute(rosterCompetition, "A", _clock);
        AddEntry.Execute(rosterCompetition, "B", _clock);
        var roster = ConfigureStructure.Execute(
            rosterCompetition,
            null,
            StructureIntent.Championship(),
            _clock).Stage;
        roster.ReplaceAffectationAuthoring(
            [.. rosterCompetition.Entries.Select(entry => entry.Id)],
            _clock);
        var rosterSchematic = StageSchematicAssembler.Assemble(roster, rosterCompetition, [roster]);
        rosterSchematic.Cases.Should().NotBeEmpty();
        rosterSchematic.Cases.Should().OnlyContain(item =>
            item.FormPosition.Kind == StageSchematicAssembler.FormKindRosterPlace
            && item.SeatMode == null
            && item.CanClear == null
            && item.Excess == null
            && item.OutsideComposition == null);
    }

    [Fact]
    public void Occupied_manual_seat_can_be_cleared_until_the_stage_has_a_match()
    {
        var (competition, stage, entryId) = Groups(places: 2, entryCount: 2);
        var other = competition.Entries.Single(entry => !entry.Id.Equals(entryId)).Id;
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[0].Id, entryId);

        var occupied = Seat(stage, competition, stage.Groups[0].Id, index: 1);
        occupied.SeatMode.Should().Be(StageSchematicAssembler.GroupSeatEditable);
        occupied.CanClear.Should().BeTrue();
        ClearEntryFromGroup.Execute(stage, competition, stage.Groups[0].Id, entryId);
        stage.Groups[0].EntryIds.Should().NotContain(entryId);

        AssignEntryToGroup.Execute(stage, competition, stage.Groups[0].Id, entryId);
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[0].Id, other);
        var match = Attach(competition, stage, entryId, other);

        var lockedClear = Seat(stage, competition, stage.Groups[0].Id, index: 1, match);
        lockedClear.SeatMode.Should().Be(StageSchematicAssembler.GroupSeatEditable);
        lockedClear.CanClear.Should().BeFalse();
        var clear = () => ClearEntryFromGroup.Execute(
            stage,
            competition,
            stage.Groups[0].Id,
            entryId,
            [match]);
        clear.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.GroupClearLockedByStageMatches);
    }

    [Fact]
    public void Qualification_or_progression_seat_refuses_clear()
    {
        var (competition, stage, entryId) = Groups(places: 2, entryCount: 1);
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[0].Id, entryId);
        stage.WriteGroupResolutionProvenance(
            stage.Groups[0].Id,
            entryId,
            new ResolutionProvenance(ResolutionProvenanceKind.Qualification, "Q|path", "fp"));

        var seat = Seat(stage, competition, stage.Groups[0].Id, index: 1);
        seat.SeatMode.Should().Be(StageSchematicAssembler.GroupSeatQualProg);
        seat.CanClear.Should().BeFalse();
        var clear = () => ClearEntryFromGroup.Execute(stage, competition, stage.Groups[0].Id, entryId);
        clear.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.GroupPlacementResolutionLocked);
    }

    [Fact]
    public void Applied_draw_seat_refuses_clear()
    {
        var (competition, stage, draw) = AppliedGroupDraw();
        var placement = draw.Resolution.GroupResults[0];

        var index = stage.GetGroup(placement.GroupId).EntryIds.ToList().IndexOf(placement.EntryId) + 1;
        var seat = Seat(stage, competition, placement.GroupId, index);
        seat.SeatMode.Should().Be(StageSchematicAssembler.GroupSeatDraw);
        seat.CanClear.Should().BeFalse();
        var clear = () => ClearEntryFromGroup.Execute(
            stage,
            competition,
            placement.GroupId,
            placement.EntryId);
        clear.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AppliedDrawGroupPlacementProtected);
    }

    [Fact]
    public void Empty_seat_in_a_group_that_has_a_match_refuses_assign()
    {
        var (competition, stage, _) = Groups(places: 4, entryCount: 3);
        var ids = competition.Entries.Select(entry => entry.Id).ToArray();
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[0].Id, ids[0]);
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[0].Id, ids[1]);
        var match = Attach(competition, stage, ids[0], ids[1]);

        var seat = Seat(stage, competition, stage.Groups[0].Id, index: 3, match);
        seat.Entry.Should().BeNull();
        seat.SeatMode.Should().Be(StageSchematicAssembler.GroupSeatMatchLocked);
        seat.CanClear.Should().BeFalse();
        var assign = () => AssignEntryToGroup.Execute(
            stage,
            competition,
            stage.Groups[0].Id,
            ids[2],
            [match]);
        assign.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.GroupAssignLockedByGroupMatches);
    }

    [Fact]
    public void Reserved_empty_seat_refuses_assign_when_free_places_do_not_exceed_pending_feeds()
    {
        var (competition, stage, source, entryId) = ReservedGroup();
        var stages = new[] { source, stage };

        var seat = Seat(stage, competition, stage.Groups[0].Id, index: 1, competitionStages: stages);
        seat.Entry.Should().BeNull();
        seat.SeatMode.Should().Be(StageSchematicAssembler.GroupSeatReserved);
        var assign = () => AssignEntryToGroup.Execute(
            stage,
            competition,
            stage.Groups[0].Id,
            entryId,
            competitionStages: stages);
        assign.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.GroupSeatReservedByInboundFeed);
    }

    [Fact]
    public void Active_group_draw_blocks_assign_and_clear_including_a_no_solution_draft()
    {
        var (competition, stage, entryId) = Groups(places: 2, entryCount: 2);
        var other = competition.Entries.Single(entry => !entry.Id.Equals(entryId)).Id;
        AssignEntryToGroup.Execute(stage, competition, stage.Groups[0].Id, entryId);
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        stage.MarkDrawNoSolution(draw.Id, _clock);
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Draft);

        var occupied = Seat(stage, competition, stage.Groups[0].Id, index: 1);
        var empty = Seat(stage, competition, stage.Groups[0].Id, index: 2);
        occupied.SeatMode.Should().Be(StageSchematicAssembler.GroupSeatDrawPending);
        occupied.CanClear.Should().BeFalse();
        empty.SeatMode.Should().Be(StageSchematicAssembler.GroupSeatDrawPending);

        var assign = () => AssignEntryToGroup.Execute(stage, competition, stage.Groups[1].Id, other);
        assign.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);
        var clear = () => ClearEntryFromGroup.Execute(stage, competition, stage.Groups[0].Id, entryId);
        clear.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);
    }

    [Fact]
    public void Fifth_member_of_a_four_place_group_is_projected_as_excess()
    {
        var competition = CreateCompetition.Execute("Excess", _clock);
        var ids = Enumerable.Range(0, 5)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 4),
            _clock).Stage;
        stage.SetPlacesPerGroup(null);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        foreach (var id in ids)
        {
            stage.AssignEntryToGroup(stage.Groups[0].Id, id);
        }

        stage.SetPlacesPerGroup(4);

        var seats = Seats(stage, competition, stage.Groups[0].Id);
        seats.Should().HaveCount(5);
        seats[4].Entry!.EntryId.Should().Be(ids[4].Value);
        seats[4].Excess.Should().BeTrue();
        seats[4].CanClear.Should().BeTrue();
        seats.Take(4).Should().OnlyContain(seat => seat.Excess == false);
        ClearEntryFromGroup.Execute(stage, competition, stage.Groups[0].Id, ids[4]);
        stage.Groups[0].EntryIds.Should().NotContain(ids[4]);

        stage.SetPlacesPerGroup(null);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[4]);
        stage.SetPlacesPerGroup(4);
        var match = Attach(competition, stage, ids[0], ids[1]);
        var locked = Seat(stage, competition, stage.Groups[0].Id, index: 5, match);
        locked.Excess.Should().BeTrue();
        locked.CanClear.Should().BeFalse();
        var clear = () => ClearEntryFromGroup.Execute(
            stage,
            competition,
            stage.Groups[0].Id,
            ids[4],
            [match]);
        clear.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.GroupClearLockedByStageMatches);
    }

    [Fact]
    public void Member_absent_from_the_composition_is_flagged_on_its_seat()
    {
        var competition = CreateCompetition.Execute("Outside", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 4),
            _clock).Stage;
        stage.ReplaceAffectationAuthoring(ids.Take(3).ToArray(), _clock);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[1]);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[2]);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[3]);

        var seats = Seats(stage, competition, stage.Groups[0].Id);
        seats.Should().HaveCount(4);
        seats.Take(3).Should().OnlyContain(seat => seat.OutsideComposition == false);
        seats[3].Entry!.EntryId.Should().Be(ids[3].Value);
        seats[3].OutsideComposition.Should().BeTrue();
        seats[3].SeatMode.Should().Be(StageSchematicAssembler.GroupSeatEditable);
        seats[3].CanClear.Should().BeTrue();
        ClearEntryFromGroup.Execute(stage, competition, stage.Groups[0].Id, ids[3]);
        stage.Groups[0].EntryIds.Should().NotContain(ids[3]);

        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[3]);
        var match = Attach(competition, stage, ids[0], ids[1]);
        var locked = Seat(stage, competition, stage.Groups[0].Id, index: 4, match);
        locked.OutsideComposition.Should().BeTrue();
        locked.CanClear.Should().BeFalse();
        var clear = () => ClearEntryFromGroup.Execute(
            stage,
            competition,
            stage.Groups[0].Id,
            ids[3],
            [match]);
        clear.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.GroupClearLockedByStageMatches);
    }

    [Fact]
    public void Running_stage_projects_editable_and_the_route_returns_structure_locked()
    {
        var (competition, stage, entryId) = Groups(places: 2, entryCount: 2);
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        stage.MarkDrawNoSolution(draw.Id, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var seat = Seat(stage, competition, stage.Groups[0].Id, index: 1);
        seat.SeatMode.Should().Be(GroupSeatModes.Editable);
        seat.CanClear.Should().BeFalse();
        var assign = () => AssignEntryToGroup.Execute(stage, competition, stage.Groups[0].Id, entryId);
        assign.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.StructureLocked);
    }

    [Fact]
    public void Repeated_put_on_a_running_stage_returns_structure_locked()
    {
        var (competition, stage, entryId) = Groups(places: 2, entryCount: 2);
        var groupId = stage.Groups[0].Id;
        AssignEntryToGroup.Execute(stage, competition, groupId, entryId);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var repeated = () => AssignEntryToGroup.Execute(stage, competition, groupId, entryId);
        var error = repeated.Should().Throw<DomainException>().Which;
        error.Code.Should().Be(StageErrorCodes.StructureLocked);
        error.Message.Should().Be(
            "Operational structure authoring cannot be modified when status is 'Running'.");
        stage.FindGroup(groupId)!.EntryIds.Should().Contain(entryId);
    }

    [Theory]
    [InlineData(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw, GroupSeatModes.DrawPending)]
    [InlineData(ApplicationErrorCodes.AppliedDrawGroupPlacementProtected, GroupSeatModes.Draw)]
    [InlineData(StageErrorCodes.GroupPlacementResolutionLocked, GroupSeatModes.QualProg)]
    [InlineData(StageErrorCodes.GroupAssignLockedByGroupMatches, GroupSeatModes.MatchLocked)]
    [InlineData(ApplicationErrorCodes.GroupSeatReservedByInboundFeed, GroupSeatModes.Reserved)]
    [InlineData(StageErrorCodes.StructureLocked, GroupSeatModes.Editable)]
    [InlineData(null, GroupSeatModes.Editable)]
    public void Refusal_code_maps_to_one_seat_mode(string? code, string mode)
    {
        GroupSeatModes.FromRefusal(code).Should().Be(mode);
    }

    private SchematicCaseDto Seat(
        Stage stage,
        Competition competition,
        GroupId groupId,
        int index,
        Match? match = null,
        IReadOnlyList<Stage>? competitionStages = null)
    {
        return Seats(stage, competition, groupId, match, competitionStages)
            .Single(item => item.FormPosition.Index == index);
    }

    private IReadOnlyList<SchematicCaseDto> Seats(
        Stage stage,
        Competition competition,
        GroupId groupId,
        Match? match = null,
        IReadOnlyList<Stage>? competitionStages = null)
    {
        _ = _clock;
        IReadOnlyList<MatchSummaryRow> rows = match is null
            ? []
            : [new MatchSummaryRow(match.Id, stage.Id, match.Status, match.HomeEntryId, match.AwayEntryId, null)];
        var schematic = StageSchematicAssembler.Assemble(
            stage,
            competition,
            competitionStages ?? [stage],
            rows);
        return [.. schematic.Cases.Where(item => item.FormPosition.GroupId == groupId.Value)];
    }

    private (Competition Competition, Stage Stage, EntryId EntryId) Groups(int places, int entryCount)
    {
        var competition = CreateCompetition.Execute($"G-{places}-{entryCount}", _clock);
        var ids = Enumerable.Range(0, entryCount)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, places),
            _clock).Stage;
        stage.ReplaceAffectationAuthoring(ids, _clock);
        return (competition, stage, ids[0]);
    }

    private (Competition Competition, Stage Destination, Stage Source, EntryId EntryId) ReservedGroup()
    {
        var competition = CreateCompetition.Execute("Reserved", _clock);
        var entryId = AddEntry.Execute(competition, "Manual", _clock).Id;
        var source = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2), _clock);
        var destination = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2), _clock);
        destination.ReplaceAffectationAuthoring([entryId], _clock);
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
        return (competition, destination, source, entryId);
    }

    private (Competition Competition, Stage Stage, Draw Draw) AppliedGroupDraw()
    {
        var competition = CreateCompetition.Execute("Applied", _clock);
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
        return (competition, stage, stage.GetDraw(draw.Id));
    }

    private Match Attach(Competition competition, Stage stage, EntryId home, EntryId away)
    {
        var matchday = stage.Matchdays.Count == 0 ? stage.AddMatchday(1, _clock) : stage.Matchdays[0];
        var fixture = stage.AddFixture(matchday.Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        return match;
    }
}
