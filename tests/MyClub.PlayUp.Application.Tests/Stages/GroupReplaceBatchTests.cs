// -----------------------------------------------------------------------
// <copyright file="GroupReplaceBatchTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
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
/// Replace keeps a full, reserved, excess, or outside-composition seat.
/// A lot stops at free places minus unresolved feeds and writes nothing when one entry is refused.
/// </summary>
public sealed class GroupReplaceBatchTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Replace_is_allowed_on_a_full_group_and_keeps_the_index()
    {
        var (competition, stage, ids) = Groups(places: 2, entryCount: 3);
        var group = stage.Groups[0];
        stage.AssignEntryToGroup(group.Id, ids[0]);
        stage.AssignEntryToGroup(group.Id, ids[1]);

        ReplaceEntryInGroup.Execute(stage, competition, group.Id, ids[0], ids[2]);

        group.EntryIds.Should().Equal(ids[2], ids[1]);
    }

    [Fact]
    public void Replace_is_allowed_for_an_excess_member()
    {
        var (competition, stage, ids) = Groups(places: 4, entryCount: 4);
        var group = stage.Groups[0];
        stage.SetPlacesPerGroup(null);
        stage.AssignEntryToGroup(group.Id, ids[0]);
        stage.AssignEntryToGroup(group.Id, ids[1]);
        stage.AssignEntryToGroup(group.Id, ids[2]);
        stage.SetPlacesPerGroup(2);

        ReplaceEntryInGroup.Execute(stage, competition, group.Id, ids[2], ids[3]);

        group.EntryIds.Should().Equal(ids[0], ids[1], ids[3]);
    }

    [Fact]
    public void Replace_ignores_a_reserved_inbound_seat()
    {
        var (competition, stage, source, ids) = ReservedGroup(places: 2, entryCount: 2);
        var group = stage.Groups[0];
        stage.AssignEntryToGroup(group.Id, ids[0]);

        var assign = () => AssignEntryToGroup.Execute(
            stage,
            competition,
            group.Id,
            ids[1],
            competitionStages: [source, stage]);
        assign.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.GroupSeatReservedByInboundFeed);

        ReplaceEntryInGroup.Execute(stage, competition, group.Id, ids[0], ids[1]);
        group.EntryIds.Should().Equal(ids[1]);
    }

    [Fact]
    public void Replace_is_allowed_when_the_occupant_is_outside_the_composition()
    {
        var competition = CreateCompetition.Execute("Outside", _clock);
        var ids = Entries(competition, 4);
        var stage = Skeleton(competition, places: 4);
        stage.ReplaceAffectationAuthoring(ids.Take(3).ToArray(), _clock);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[3]);

        ReplaceEntryInGroup.Execute(stage, competition, stage.Groups[0].Id, ids[3], ids[0]);

        stage.Groups[0].EntryIds.Should().Equal(ids[0]);
    }

    [Fact]
    public void Replace_same_team_has_no_effect()
    {
        var (competition, stage, ids) = Groups(places: 2, entryCount: 1);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);

        ReplaceEntryInGroup.Execute(stage, competition, stage.Groups[0].Id, ids[0], ids[0]);

        stage.Groups[0].EntryIds.Should().Equal(ids[0]);
    }

    [Fact]
    public void Replace_refuses_a_running_stage()
    {
        var (competition, stage, ids) = Groups(places: 2, entryCount: 2);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => ReplaceEntryInGroup.Execute(stage, competition, stage.Groups[0].Id, ids[0], ids[1]);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        stage.Groups[0].EntryIds.Should().Equal(ids[0]);
    }

    [Fact]
    public void Replace_refuses_while_a_group_draw_is_not_applied()
    {
        var (competition, stage, ids) = Groups(places: 2, entryCount: 4);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random, potRules: new PotRules(2)), _clock);
        var draft = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        ConfigureDrawInputs.Execute(stage, draft.Id, DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group));
        GenerateDrawResolution.Execute(
            stage,
            draft.Id,
            _clock,
            groupTargets: [.. stage.Groups.Select(group => group.Id)]).IsResolved.Should().BeTrue();

        var blocked = () => ReplaceEntryInGroup.Execute(stage, competition, stage.Groups[0].Id, ids[0], ids[1]);
        blocked.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);

        PublishDraw.Execute(stage, draft.Id, _clock);
        var published = () => ReplaceEntryInGroup.Execute(stage, competition, stage.Groups[0].Id, ids[0], ids[1]);
        published.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);
        stage.Groups[0].EntryIds.Should().Equal(ids[0]);
    }

    [Fact]
    public void Replace_refuses_an_applied_draw_on_the_occupant_and_on_the_replacement()
    {
        var (competition, stage, draw) = AppliedDraw();
        var outgoing = draw.Resolution.GroupResults[0];
        var incoming = draw.Resolution.GroupResults.First(row => !row.GroupId.Equals(outgoing.GroupId));

        var clearSide = () => ReplaceEntryInGroup.Execute(
            stage,
            competition,
            outgoing.GroupId,
            outgoing.EntryId,
            incoming.EntryId);
        clearSide.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AppliedDrawGroupPlacementProtected);

        var extra = AddEntry.Execute(competition, "Manual", _clock).Id;
        stage.ReplaceAffectationAuthoring(
            [.. stage.CompositionEntries.Select(entry => entry.EntryId), extra],
            _clock);
        stage.SetPlacesPerGroup(4);
        stage.AssignEntryToGroup(outgoing.GroupId, extra);
        var assignSide = () => ReplaceEntryInGroup.Execute(
            stage,
            competition,
            outgoing.GroupId,
            extra,
            incoming.EntryId);
        assignSide.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AppliedDrawGroupPlacementProtected);
        stage.GetGroup(outgoing.GroupId).EntryIds.Should().Contain(extra);
    }

    [Fact]
    public void Replace_refuses_qualification_provenance_on_the_occupant_and_on_the_replacement()
    {
        var (competition, stage, ids) = Groups(places: 2, entryCount: 3);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        stage.AssignEntryToGroup(stage.Groups[1].Id, ids[1]);
        var provenance = new ResolutionProvenance(ResolutionProvenanceKind.Qualification, "Q|path", "fp");
        stage.WriteGroupResolutionProvenance(stage.Groups[0].Id, ids[0], provenance);

        var outgoing = () => ReplaceEntryInGroup.Execute(
            stage,
            competition,
            stage.Groups[0].Id,
            ids[0],
            ids[2]);
        outgoing.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.GroupPlacementResolutionLocked);
        stage.Groups[0].EntryIds.Should().Equal(ids[0]);

        var (otherCompetition, otherStage, otherIds) = Groups(places: 2, entryCount: 3);
        otherStage.AssignEntryToGroup(otherStage.Groups[0].Id, otherIds[0]);
        otherStage.AssignEntryToGroup(otherStage.Groups[1].Id, otherIds[1]);
        otherStage.WriteGroupResolutionProvenance(otherStage.Groups[1].Id, otherIds[1], provenance);
        var incoming = () => ReplaceEntryInGroup.Execute(
            otherStage,
            otherCompetition,
            otherStage.Groups[0].Id,
            otherIds[0],
            otherIds[1]);
        incoming.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.GroupPlacementResolutionLocked);
        otherStage.Groups[0].EntryIds.Should().Equal(otherIds[0]);
    }

    [Fact]
    public void Replace_refuses_when_any_group_on_the_phase_has_a_match()
    {
        var (competition, stage, ids) = Groups(places: 2, entryCount: 4);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        stage.AssignEntryToGroup(stage.Groups[1].Id, ids[2]);
        stage.AssignEntryToGroup(stage.Groups[1].Id, ids[3]);
        var match = Attach(competition, stage, ids[2], ids[3]);

        var act = () => ReplaceEntryInGroup.Execute(
            stage,
            competition,
            stage.Groups[0].Id,
            ids[0],
            ids[1],
            [match]);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.GroupClearLockedByStageMatches);
        stage.Groups[0].EntryIds.Should().Equal(ids[0]);
    }

    [Fact]
    public void Replace_refuses_a_replacement_outside_the_composition_or_already_in_another_group()
    {
        var competition = CreateCompetition.Execute("Refuse", _clock);
        var ids = Entries(competition, 3);
        var outsider = AddEntry.Execute(competition, "Out", _clock).Id;
        var stage = Skeleton(competition, places: 2);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        stage.AssignEntryToGroup(stage.Groups[1].Id, ids[1]);

        var missing = () => ReplaceEntryInGroup.Execute(
            stage,
            competition,
            stage.Groups[0].Id,
            ids[0],
            outsider);
        missing.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.GroupAssignmentEntryNotInComposition);

        var duplicate = () => ReplaceEntryInGroup.Execute(
            stage,
            competition,
            stage.Groups[0].Id,
            ids[0],
            ids[1]);
        duplicate.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DuplicateEntry);
        stage.Groups[0].EntryIds.Should().Equal(ids[0]);
    }

    [Fact]
    public void Replace_refuses_when_the_occupant_is_already_gone()
    {
        var (competition, stage, ids) = Groups(places: 2, entryCount: 2);

        var act = () => ReplaceEntryInGroup.Execute(stage, competition, stage.Groups[0].Id, ids[0], ids[1]);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.EntryNotFound);
    }

    [Fact]
    public void Replace_replay_is_a_success_when_the_replacement_is_already_seated()
    {
        var (competition, stage, ids) = Groups(places: 2, entryCount: 3);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        ReplaceEntryInGroup.Execute(stage, competition, stage.Groups[0].Id, ids[0], ids[1]);

        ReplaceEntryInGroup.Execute(stage, competition, stage.Groups[0].Id, ids[0], ids[1]);

        stage.Groups[0].EntryIds.Should().Equal(ids[1]);
    }

    [Fact]
    public void Replace_replay_is_refused_on_a_running_stage()
    {
        var (competition, stage, ids) = Groups(places: 2, entryCount: 3);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        ReplaceEntryInGroup.Execute(stage, competition, stage.Groups[0].Id, ids[0], ids[1]);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var replay = () => ReplaceEntryInGroup.Execute(stage, competition, stage.Groups[0].Id, ids[0], ids[1]);

        replay.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        stage.Groups[0].EntryIds.Should().Equal(ids[1]);
    }

    [Fact]
    public void Replace_replay_is_refused_while_a_group_draw_is_not_applied()
    {
        var (competition, stage, ids) = Groups(places: 2, entryCount: 4);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        ReplaceEntryInGroup.Execute(stage, competition, stage.Groups[0].Id, ids[0], ids[1]);
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random, potRules: new PotRules(2)), _clock);
        var draft = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        ConfigureDrawInputs.Execute(stage, draft.Id, DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group));
        GenerateDrawResolution.Execute(
            stage,
            draft.Id,
            _clock,
            groupTargets: [.. stage.Groups.Select(group => group.Id)]).IsResolved.Should().BeTrue();

        var replay = () => ReplaceEntryInGroup.Execute(stage, competition, stage.Groups[0].Id, ids[0], ids[1]);

        replay.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);
        stage.Groups[0].EntryIds.Should().Equal(ids[1]);
    }

    [Fact]
    public void Batch_appends_in_body_order_and_ignores_an_entry_already_in_the_group()
    {
        var (competition, stage, ids) = Groups(places: 4, entryCount: 3);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);

        AssignEntriesToGroup.Execute(stage, competition, stage.Groups[0].Id, [ids[2], ids[0], ids[1]]);

        stage.Groups[0].EntryIds.Should().Equal(ids[0], ids[2], ids[1]);
    }

    [Fact]
    public void Batch_refuses_without_mutation_when_the_list_exceeds_free_places()
    {
        var (competition, stage, ids) = Groups(places: 2, entryCount: 3);

        var act = () => AssignEntriesToGroup.Execute(stage, competition, stage.Groups[0].Id, ids);

        var error = act.Should().Throw<GroupPlacementRejectedException>().Which;
        error.Code.Should().Be(StageErrorCodes.GroupFull);
        error.EntryId.Should().Be(ids[2]);
        stage.Groups[0].EntryIds.Should().BeEmpty();
    }

    [Fact]
    public void Batch_refuses_without_mutation_when_a_seat_is_reserved_for_a_feed()
    {
        var (competition, stage, source, ids) = ReservedGroup(places: 4, entryCount: 4);

        var act = () => AssignEntriesToGroup.Execute(
            stage,
            competition,
            stage.Groups[0].Id,
            ids,
            competitionStages: [source, stage]);

        var error = act.Should().Throw<GroupPlacementRejectedException>().Which;
        error.Code.Should().Be(ApplicationErrorCodes.GroupSeatReservedByInboundFeed);
        error.EntryId.Should().Be(ids[3]);
        stage.Groups[0].EntryIds.Should().BeEmpty();
    }

    [Fact]
    public void Batch_of_the_unreserved_places_is_accepted()
    {
        var (competition, stage, source, ids) = ReservedGroup(places: 4, entryCount: 3);

        AssignEntriesToGroup.Execute(
            stage,
            competition,
            stage.Groups[0].Id,
            ids,
            competitionStages: [source, stage]);

        stage.Groups[0].EntryIds.Should().Equal(ids);
    }

    [Fact]
    public void Batch_refuses_when_places_per_group_is_absent()
    {
        var (competition, stage, ids) = Groups(places: 2, entryCount: 1);
        stage.SetPlacesPerGroup(null);

        var act = () => AssignEntriesToGroup.Execute(stage, competition, stage.Groups[0].Id, ids);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.InvalidStructureIntent);
        stage.Groups[0].EntryIds.Should().BeEmpty();
    }

    [Fact]
    public void Batch_refuses_zero_places_and_a_running_stage_before_that_check()
    {
        var (competition, stage, ids) = Groups(places: 2, entryCount: 1);
        var setter = () => stage.SetPlacesPerGroup(0);
        setter.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidConfiguration);

        SetPlacesPerGroup(stage, 0);
        var zero = () => AssignEntriesToGroup.Execute(stage, competition, stage.Groups[0].Id, ids);
        zero.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.InvalidStructureIntent);
        stage.Groups[0].EntryIds.Should().BeEmpty();

        SetPlacesPerGroup(stage, null);
        stage.Prepare(_clock);
        stage.Start(_clock);
        var running = () => AssignEntriesToGroup.Execute(stage, competition, stage.Groups[0].Id, ids);
        running.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        stage.Groups[0].EntryIds.Should().BeEmpty();
    }

    [Fact]
    public void Batch_unknown_group_names_no_entry()
    {
        var (competition, stage, ids) = Groups(places: 2, entryCount: 2);

        var act = () => AssignEntriesToGroup.Execute(stage, competition, GroupId.New(), ids);

        var error = act.Should().Throw<DomainException>().Which;
        error.Should().BeOfType<DomainException>();
        error.Code.Should().Be(StageErrorCodes.GroupNotFound);
        stage.Groups[0].EntryIds.Should().BeEmpty();
    }

    [Fact]
    public void Can_clear_matches_a_replace_of_a_valid_replacement()
    {
        var (competition, stage, ids) = Groups(places: 2, entryCount: 3);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        var open = Seat(stage, competition, stage.Groups[0].Id, index: 1);
        open.CanClear.Should().BeTrue();

        ReplaceEntryInGroup.Execute(stage, competition, stage.Groups[0].Id, ids[0], ids[1]);
        stage.Groups[0].EntryIds.Should().Equal(ids[1]);

        var match = Attach(competition, stage, ids[1], ids[2]);
        stage.AssignEntryToGroup(stage.Groups[1].Id, ids[0]);
        var locked = Seat(stage, competition, stage.Groups[1].Id, index: 1, match);
        locked.CanClear.Should().BeFalse();
        var refused = () => ReplaceEntryInGroup.Execute(
            stage,
            competition,
            stage.Groups[1].Id,
            ids[0],
            ids[2],
            [match]);
        refused.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.GroupClearLockedByStageMatches);
    }

    private static void SetPlacesPerGroup(Stage stage, int? places) =>
        typeof(Stage).GetProperty(nameof(Stage.PlacesPerGroup))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(stage, [places]);

    private static SchematicCaseDto Seat(
        Stage stage,
        Competition competition,
        GroupId groupId,
        int index,
        Match? match = null,
        IReadOnlyList<Stage>? competitionStages = null)
    {
        IReadOnlyList<MatchSummaryRow> rows = match is null
            ? []
            : [new MatchSummaryRow(match.Id, stage.Id, match.Status, match.HomeEntryId, match.AwayEntryId, null)];
        var schematic = StageSchematicAssembler.Assemble(
            stage,
            competition,
            competitionStages ?? [stage],
            rows);
        return schematic.Cases.Single(item =>
            item.FormPosition.GroupId == groupId.Value && item.FormPosition.Index == index);
    }

    private (Competition Competition, Stage Stage, EntryId[] Ids) Groups(int places, int entryCount)
    {
        var competition = CreateCompetition.Execute($"G-{places}-{entryCount}", _clock);
        var ids = Entries(competition, entryCount);
        var stage = Skeleton(competition, places);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        return (competition, stage, ids);
    }

    private (Competition Competition, Stage Destination, Stage Source, EntryId[] Ids) ReservedGroup(
        int places,
        int entryCount)
    {
        var competition = CreateCompetition.Execute($"Reserved-{places}", _clock);
        var ids = Entries(competition, entryCount);
        var source = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2), _clock);
        var destination = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, places), _clock);
        destination.ReplaceAffectationAuthoring(ids, _clock);
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
        return (competition, destination, source, ids);
    }

    private (Competition Competition, Stage Stage, Draw Draw) AppliedDraw()
    {
        var competition = CreateCompetition.Execute("Applied", _clock);
        var ids = Entries(competition, 4);
        var stage = Skeleton(competition, 2);
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random, potRules: new PotRules(2)), _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        ConfigureDrawInputs.Execute(stage, draw.Id, DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group));
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
        stage.Prepare(_clock);
        return match;
    }

    private Stage Skeleton(Competition competition, int places) =>
        ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, places), _clock).Stage;

    private EntryId[] Entries(Competition competition, int count) =>
        Enumerable.Range(0, count).Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id).ToArray();
}
