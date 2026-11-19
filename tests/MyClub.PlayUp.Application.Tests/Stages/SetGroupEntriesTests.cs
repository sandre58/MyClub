// -----------------------------------------------------------------------
// <copyright file="SetGroupEntriesTests.cs" company="Stéphane ANDRE">
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

public sealed class SetGroupEntriesTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Set_same_membership_is_noop_without_domain_write()
    {
        var (competition, stage, ids) = Groups(places: 4, entryCount: 3);
        var group = stage.Groups[0];
        stage.AssignEntryToGroup(group.Id, ids[0]);
        stage.AssignEntryToGroup(group.Id, ids[1]);

        var mutated = SetGroupEntries.Execute(
            stage,
            competition,
            group.Id,
            [ids[0], ids[1]],
            expectedEntryIds: null);

        mutated.Should().BeFalse();
        group.EntryIds.Should().Equal(ids[0], ids[1]);
    }

    [Fact]
    public void Set_reorders_additions_at_end_and_keeps_conserved_order()
    {
        var (competition, stage, ids) = Groups(places: 4, entryCount: 4);
        var group = stage.Groups[0];
        stage.AssignEntryToGroup(group.Id, ids[0]);
        stage.AssignEntryToGroup(group.Id, ids[1]);

        _ = SetGroupEntries.Execute(
            stage,
            competition,
            group.Id,
            [ids[1], ids[2], ids[0]],
            [ids[0], ids[1]]);

        group.EntryIds.Should().Equal(ids[0], ids[1], ids[2]);
    }

    [Fact]
    public void Set_stale_expected_refuses_without_mutation()
    {
        var (competition, stage, ids) = Groups(places: 4, entryCount: 3);
        var group = stage.Groups[0];
        stage.AssignEntryToGroup(group.Id, ids[0]);

        var act = () => SetGroupEntries.Execute(
            stage,
            competition,
            group.Id,
            [ids[1]],
            [ids[0], ids[1]]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.GroupMembershipStale);
        group.EntryIds.Should().Equal(ids[0]);
    }

    [Fact]
    public void Set_expected_order_is_ignored_for_stale_check()
    {
        var (competition, stage, ids) = Groups(places: 4, entryCount: 3);
        var group = stage.Groups[0];
        stage.AssignEntryToGroup(group.Id, ids[0]);
        stage.AssignEntryToGroup(group.Id, ids[1]);

        _ = SetGroupEntries.Execute(
            stage,
            competition,
            group.Id,
            [ids[1], ids[2]],
            [ids[1], ids[0]]);

        group.EntryIds.Should().Equal(ids[1], ids[2]);
    }

    [Fact]
    public void Set_missing_expected_on_mutation_returns_400()
    {
        var (competition, stage, ids) = Groups(places: 4, entryCount: 2);
        var group = stage.Groups[0];

        var act = () => SetGroupEntries.Execute(stage, competition, group.Id, [ids[0]], null);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.InvalidStructureIntent);
    }

    [Fact]
    public void Set_outside_composition_member_identical_list_is_noop()
    {
        var competition = CreateCompetition.Execute("Outside-set", _clock);
        var ids = Entries(competition, 4);
        var stage = Skeleton(competition, 4);
        stage.ReplaceAffectationAuthoring(ids.Take(3).ToArray(), _clock);
        var group = stage.Groups[0];
        stage.AssignEntryToGroup(group.Id, ids[3]);

        var mutated = SetGroupEntries.Execute(
            stage,
            competition,
            group.Id,
            [ids[3]],
            expectedEntryIds: null);

        mutated.Should().BeFalse();
        group.EntryIds.Should().Equal(ids[3]);
    }

    [Fact]
    public void Hub_action_SetGroupEntries_follows_groups_draft_and_ready()
    {
        var competition = CreateCompetition.Execute("Actions-set", _clock);
        var groups = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock).Stage;
        var draft = StructureViewAssembler.Assemble(competition, [groups]).Stages.Single();
        draft.Actions.Should().Contain(StructureViewAssembler.ActionSetGroupEntries);

        var ids = Entries(competition, 4);
        groups.ReplaceAffectationAuthoring(ids, _clock);
        groups.Prepare(_clock);
        var ready = StructureViewAssembler.Assemble(competition, [groups]).Stages.Single();
        ready.Actions.Should().Contain(StructureViewAssembler.ActionSetGroupEntries);

        groups.Start(_clock);
        var running = StructureViewAssembler.Assemble(competition, [groups]).Stages.Single();
        running.Actions.Should().NotContain(StructureViewAssembler.ActionSetGroupEntries);
    }

    [Fact]
    public void Ceiling_4_of_4_with_one_feed_swap_one_for_one_succeeds()
    {
        var (competition, destination, source, ids) = ReservedGroup(places: 4, entryCount: 5);
        var group = destination.Groups[0];
        foreach (var entryId in ids.Take(4))
        {
            destination.AssignEntryToGroup(group.Id, entryId);
        }

        var current = group.EntryIds.ToArray();
        _ = SetGroupEntries.Execute(
            destination,
            competition,
            group.Id,
            [current[1], current[0], current[2], current[3]],
            current,
            competitionStages: [source, destination]);
    }

    [Fact]
    public void Ceiling_5_of_4_identical_set_is_noop()
    {
        var (competition, stage, ids, group) = ExcessGroup(places: 4, memberCount: 5, entryCount: 6);
        var current = group.EntryIds.ToArray();
        var mutated = SetGroupEntries.Execute(stage, competition, group.Id, current, null);
        mutated.Should().BeFalse();
    }

    [Fact]
    public void Ceiling_5_of_4_plus_one_is_group_full()
    {
        var (competition, stage, ids, group) = ExcessGroup(places: 4, memberCount: 5, entryCount: 7);
        var current = group.EntryIds.ToArray();
        var act = () => SetGroupEntries.Execute(stage, competition, group.Id, current.Append(ids[5]).ToArray(), current);
        act.Should().Throw<GroupPlacementRejectedException>().Which.Code.Should().Be(StageErrorCodes.GroupFull);
    }

    [Fact]
    public void Ceiling_4_of_3_with_one_feed_plus_one_is_reserved()
    {
        var (competition, destination, source, ids) = ReservedGroup(places: 3, entryCount: 6);
        var group = destination.Groups[0];
        destination.SetPlacesPerGroup(null);
        foreach (var entryId in ids.Take(4))
        {
            destination.AssignEntryToGroup(group.Id, entryId);
        }

        destination.SetPlacesPerGroup(3);

        var current = group.EntryIds.ToArray();
        var act = () => SetGroupEntries.Execute(
            destination,
            competition,
            group.Id,
            current.Append(ids[4]).ToArray(),
            current,
            competitionStages: [source, destination]);
        act.Should().Throw<GroupPlacementRejectedException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.GroupSeatReservedByInboundFeed);
    }

    [Fact]
    public void Ceiling_5_of_4_swap_one_for_one_succeeds()
    {
        var (competition, stage, ids, group) = ExcessGroup(places: 4, memberCount: 5, entryCount: 6);
        var current = group.EntryIds.ToArray();
        _ = SetGroupEntries.Execute(
            stage,
            competition,
            group.Id,
            [current[1], current[2], current[3], current[4], ids[5]],
            current);
        group.EntryIds.Should().EndWith(ids[5]);
    }

    [Fact]
    public void Ceiling_5_of_4_minus_two_plus_one_succeeds()
    {
        var (competition, stage, ids, group) = ExcessGroup(places: 4, memberCount: 5, entryCount: 6);
        var current = group.EntryIds.ToArray();
        _ = SetGroupEntries.Execute(
            stage,
            competition,
            group.Id,
            [current[0], current[1], current[2], ids[5]],
            current);
        group.EntryIds.Should().Equal(current[0], current[1], current[2], ids[5]);
    }

    [Fact]
    public void Ceiling_5_of_4_minus_two_plus_two_succeeds()
    {
        var (competition, stage, ids, group) = ExcessGroup(places: 4, memberCount: 5, entryCount: 7);
        var current = group.EntryIds.ToArray();
        _ = SetGroupEntries.Execute(
            stage,
            competition,
            group.Id,
            [current[0], current[1], current[2], ids[5], ids[6]],
            current);
        group.EntryIds.Should().Equal(current[0], current[1], current[2], ids[5], ids[6]);
    }

    [Fact]
    public void Ceiling_4_of_2_with_three_feeds_plus_one_is_reserved()
    {
        var (competition, destination, source, ids) = ReservedGroup(places: 2, entryCount: 8, feeds: 3);
        var group = destination.Groups[0];
        destination.SetPlacesPerGroup(null);
        foreach (var entryId in ids.Take(4))
        {
            destination.AssignEntryToGroup(group.Id, entryId);
        }

        destination.SetPlacesPerGroup(2);

        var current = group.EntryIds.ToArray();
        var act = () => SetGroupEntries.Execute(
            destination,
            competition,
            group.Id,
            current.Append(ids[4]).ToArray(),
            current,
            competitionStages: [source, destination]);
        act.Should().Throw<GroupPlacementRejectedException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.GroupSeatReservedByInboundFeed);
    }

    private (Competition Competition, Stage Stage, EntryId[] Ids, Group Group) ExcessGroup(
        int places,
        int memberCount,
        int entryCount)
    {
        var (competition, stage, ids) = Groups(places, entryCount);
        var group = stage.Groups[0];
        stage.SetPlacesPerGroup(null);
        foreach (var entryId in ids.Take(memberCount))
        {
            stage.AssignEntryToGroup(group.Id, entryId);
        }

        stage.SetPlacesPerGroup(places);
        return (competition, stage, ids, group);
    }

    private (Competition Competition, Stage Stage, EntryId[] Ids) Groups(int places, int entryCount)
    {
        var competition = CreateCompetition.Execute($"Set-{places}-{entryCount}", _clock);
        var ids = Entries(competition, entryCount);
        var stage = Skeleton(competition, places);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        return (competition, stage, ids);
    }

    private (Competition Competition, Stage Destination, Stage Source, EntryId[] Ids) ReservedGroup(
        int places,
        int entryCount,
        int feeds = 1)
    {
        var competition = CreateCompetition.Execute($"Set-res-{places}-{feeds}", _clock);
        var ids = Entries(competition, entryCount);
        var source = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2), _clock);
        var destination = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, places), _clock);
        destination.ReplaceAffectationAuthoring(ids, _clock);
        var paths = new List<QualificationPath>();
        for (var index = 0; index < feeds; index++)
        {
            paths.Add(new QualificationPath(
                index + 1,
                QualificationSource.FromGroup(source.Groups[0].Id),
                new QualificationSelection(SelectionMode.Position, index + 1),
                QualificationDestination.ForGroup(destination.Id, destination.Groups[0].Id)));
        }

        source.ReplaceQualificationRules(new QualificationRules(paths), _clock);
        return (competition, destination, source, ids);
    }

    private Stage Skeleton(Competition competition, int places) =>
        ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, places), _clock).Stage;

    private EntryId[] Entries(Competition competition, int count) =>
        Enumerable.Range(0, count).Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id).ToArray();
}