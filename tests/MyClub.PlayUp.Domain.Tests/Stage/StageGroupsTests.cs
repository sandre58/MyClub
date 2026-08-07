// -----------------------------------------------------------------------
// <copyright file="StageGroupsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stage;
using MyClub.PlayUp.Domain.Stage.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;

namespace MyClub.PlayUp.Domain.Tests.Stage;

public sealed class StageGroupsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 6, 12, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void AssignEntryToGroup_adds_entry_to_group()
    {
        // Arrange
        var stage = StageAggregate.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var entryId = EntryId.New();
        stage.ClearDomainEvents();

        // Act
        stage.AssignEntryToGroup(group.Id, entryId, _clock);

        // Assert
        group.EntryIds.Should().ContainSingle().Which.Should().Be(entryId);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AssignEntryToGroup_same_group_twice_is_noop()
    {
        // Arrange
        var stage = StageAggregate.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var entryId = EntryId.New();
        stage.AssignEntryToGroup(group.Id, entryId, _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        stage.ClearDomainEvents();

        // Act
        stage.AssignEntryToGroup(group.Id, entryId, _clock);

        // Assert
        stage.Status.Should().Be(StageStatus.Ready);
        group.EntryIds.Should().ContainSingle();
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AssignEntryToGroup_cross_group_duplicate_is_rejected()
    {
        // Arrange
        var stage = StageAggregate.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var groupA = stage.AddGroup("A", _clock);
        var groupB = stage.AddGroup("B", _clock);
        var entryId = EntryId.New();
        stage.AssignEntryToGroup(groupA.Id, entryId, _clock);

        // Act
        var act = () => stage.AssignEntryToGroup(groupB.Id, entryId, _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DuplicateEntry);
    }

    [Fact]
    public void AssignEntryToGroup_demotes_Ready_when_new_assignment()
    {
        // Arrange
        var stage = CreateReadyPoules();
        var group = stage.Groups[0];
        stage.ClearDomainEvents();

        // Act
        stage.AssignEntryToGroup(group.Id, EntryId.New(), _clock);

        // Assert
        stage.Status.Should().Be(StageStatus.Draft);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RemoveEntryFromGroup_removes_entry_and_demotes_Ready()
    {
        // Arrange
        var stage = StageAggregate.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var entryId = EntryId.New();
        stage.AssignEntryToGroup(group.Id, entryId, _clock);
        stage.AssignEntryToGroup(group.Id, EntryId.New(), _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        stage.ClearDomainEvents();

        // Act
        stage.RemoveEntryFromGroup(group.Id, entryId, _clock);

        // Assert
        group.EntryIds.Should().NotContain(entryId);
        stage.Status.Should().Be(StageStatus.Draft);
    }

    [Fact]
    public void RemoveEntryFromGroup_missing_entry_is_rejected()
    {
        // Arrange
        var stage = StageAggregate.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);

        // Act
        var act = () => stage.RemoveEntryFromGroup(group.Id, EntryId.New(), _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.EntryNotFound);
    }

    [Fact]
    public void ArrangeGroups_permutation_reorders_and_demotes_Ready()
    {
        // Arrange
        var stage = StageAggregate.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var a = stage.AddGroup("A", _clock);
        var b = stage.AddGroup("B", _clock);
        stage.AssignEntryToGroup(a.Id, EntryId.New(), _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        stage.ClearDomainEvents();

        // Act
        stage.ArrangeGroups([b.Id, a.Id], _clock);

        // Assert
        stage.Groups.Select(g => g.Id).Should().Equal(b.Id, a.Id);
        stage.Status.Should().Be(StageStatus.Draft);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ArrangeGroups_identical_order_is_noop()
    {
        // Arrange
        var stage = CreateReadyPoules();
        var order = stage.Groups.Select(g => g.Id).ToList();
        stage.ClearDomainEvents();

        // Act
        stage.ArrangeGroups(order, _clock);

        // Assert
        stage.Status.Should().Be(StageStatus.Ready);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ArrangeGroups_invalid_permutation_is_rejected()
    {
        // Arrange
        var stage = StageAggregate.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var a = stage.AddGroup("A", _clock);

        // Act
        var act = () => stage.ArrangeGroups([a.Id, GroupId.New()], _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidOrder);
    }

    [Fact]
    public void GetGroup_unknown_id_is_rejected()
    {
        // Arrange
        var stage = StageAggregate.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);

        // Act
        var act = () => stage.GetGroup(GroupId.New());

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.GroupNotFound);
    }

    [Fact]
    public void Events_order_Create_then_AddGroup_then_Prepare()
    {
        // Arrange & Act
        var stage = StageAggregate.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        stage.AssignEntryToGroup(group.Id, EntryId.New(), _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);

        // Assert
        stage.DomainEvents.Select(e => e.GetType()).Should().Equal(
            typeof(StageCreated),
            typeof(StageGroupAdded),
            typeof(StageMatchdayAdded),
            typeof(StagePrepared));
    }

    [Fact]
    public void ArrangeRounds_and_Matchdays_follow_same_rules()
    {
        // Arrange — rounds
        var cup = StageAggregate.Create(_competitionId, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        var r1 = cup.AddRound("QF", _clock);
        var r2 = cup.AddRound("SF", _clock);
        cup.Prepare(_clock);
        cup.ClearDomainEvents();
        cup.ArrangeRounds([r1.Id, r2.Id], _clock);
        cup.Status.Should().Be(StageStatus.Ready);
        cup.ArrangeRounds([r2.Id, r1.Id], _clock);
        cup.Status.Should().Be(StageStatus.Draft);

        // Arrange — matchdays
        var league = StageAggregate.Create(_competitionId, new StageName("League"), SampleRegulations.Standard(), _clock);
        var m1 = league.AddMatchday(1, _clock);
        var m2 = league.AddMatchday(2, _clock);
        league.Prepare(_clock);
        league.ClearDomainEvents();
        league.ArrangeMatchdays([m1.Id, m2.Id], _clock);
        league.Status.Should().Be(StageStatus.Ready);
        league.ArrangeMatchdays([m2.Id, m1.Id], _clock);
        league.Status.Should().Be(StageStatus.Draft);
        league.Matchdays.Select(m => m.Id).Should().Equal(m2.Id, m1.Id);
    }

    private StageAggregate CreateReadyPoules()
    {
        var stage = StageAggregate.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        stage.AssignEntryToGroup(group.Id, EntryId.New(), _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        return stage;
    }
}
