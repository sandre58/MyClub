// -----------------------------------------------------------------------
// <copyright file="StageGroupsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Stages.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class StageGroupsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 6, 12, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void AssignEntryToGroup_adds_entry_to_group()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var entryId = EntryId.New();
        stage.ClearDomainEvents();

        // Act
        stage.AssignEntryToGroup(group.Id, entryId);

        // Assert
        group.EntryIds.Should().ContainSingle().Which.Should().Be(entryId);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AssignEntryToGroup_same_group_twice_is_noop()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var entryId = EntryId.New();
        stage.AssignEntryToGroup(group.Id, entryId);
        stage.AddMatchday(1, _clock);
        stage.ClearDomainEvents();

        stage.AssignEntryToGroup(group.Id, entryId);

        stage.Status.Should().Be(StageStatus.Draft);
        group.EntryIds.Should().ContainSingle();
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AssignEntryToGroup_rejects_capacity_overflow()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        _ = stage.AddGroup("A", _clock);
        stage.SetPlacesPerGroup(2);
        var e1 = EntryId.New();
        var e2 = EntryId.New();
        var e3 = EntryId.New();
        stage.AssignEntryToGroup(stage.Groups[0].Id, e1);
        stage.AssignEntryToGroup(stage.Groups[0].Id, e2);

        var act = () => stage.AssignEntryToGroup(stage.Groups[0].Id, e3);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.GroupFull);
    }

    [Fact]
    public void AssignEntryToGroup_cross_group_duplicate_is_rejected()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var groupA = stage.AddGroup("A", _clock);
        var groupB = stage.AddGroup("B", _clock);
        var entryId = EntryId.New();
        stage.AssignEntryToGroup(groupA.Id, entryId);

        // Act
        var act = () => stage.AssignEntryToGroup(groupB.Id, entryId);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DuplicateEntry);
    }

    [Fact]
    public void AssignEntryToGroup_in_Ready_is_allowed()
    {
        var stage = CreateReadyPoules();
        var group = stage.Groups[0];
        var entryId = EntryId.New();

        stage.AssignEntryToGroup(group.Id, entryId);

        group.EntryIds.Should().Contain(entryId);
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void RemoveEntryFromGroup_in_Ready_without_a_match_is_allowed()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var entryId = EntryId.New();
        stage.AssignEntryToGroup(group.Id, entryId);
        stage.AssignEntryToGroup(group.Id, EntryId.New());
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);

        stage.RemoveEntryFromGroup(group.Id, entryId);

        group.EntryIds.Should().NotContain(entryId);
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Manual_clear_is_refused_when_another_group_has_a_match(bool prepare)
    {
        var stage = StageWithTwoGroupsAndAMatchOnTheFirst(prepare, out var quietGroup, out var entryOnQuietGroup, out _);

        var act = () => stage.RemoveEntryFromGroup(quietGroup.Id, entryOnQuietGroup);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.GroupClearLockedByStageMatches);
        quietGroup.EntryIds.Should().Contain(entryOnQuietGroup);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Manual_assign_is_refused_when_the_target_group_has_a_match(bool prepare)
    {
        var stage = StageWithTwoGroupsAndAMatchOnTheFirst(prepare, out _, out _, out var snapshots);
        var newcomer = EntryId.New();
        stage.ReplaceAffectationAuthoringOperationally(
            [.. stage.AffectationAuthoring.Select(entry => entry.EntryId), newcomer],
            _clock);

        var act = () => stage.AssignManualEntryToGroup(stage.Groups[0].Id, newcomer, snapshots);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.GroupAssignLockedByGroupMatches);
        stage.Groups[0].EntryIds.Should().NotContain(newcomer);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Manual_assign_is_allowed_when_only_another_group_has_a_match(bool prepare)
    {
        var stage = StageWithTwoGroupsAndAMatchOnTheFirst(prepare, out var quietGroup, out _, out var snapshots);
        var newcomer = EntryId.New();
        stage.ReplaceAffectationAuthoringOperationally(
            [.. stage.AffectationAuthoring.Select(entry => entry.EntryId), newcomer],
            _clock);

        stage.AssignManualEntryToGroup(quietGroup.Id, newcomer, snapshots);

        quietGroup.EntryIds.Should().Contain(newcomer);
    }

    [Fact]
    public void Manual_assign_of_an_entry_already_in_a_group_with_a_match_is_idempotent()
    {
        var stage = StageWithTwoGroupsAndAMatchOnTheFirst(prepare: false, out _, out _, out var snapshots);
        var alreadyThere = stage.Groups[0].EntryIds[0];

        stage.AssignManualEntryToGroup(stage.Groups[0].Id, alreadyThere, snapshots);

        stage.Groups[0].EntryIds.Should().Contain(alreadyThere);
    }

    [Fact]
    public void AssignEntryToGroup_in_Running_is_rejected()
    {
        var stage = CreateReadyPoules();
        stage.Start(_clock);
        var act = () => stage.AssignEntryToGroup(stage.Groups[0].Id, EntryId.New());

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
    }

    [Fact]
    public void RemoveEntryFromGroup_in_Running_is_rejected()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var entryId = EntryId.New();
        stage.AssignEntryToGroup(group.Id, entryId);
        stage.AssignEntryToGroup(group.Id, EntryId.New());
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => stage.RemoveEntryFromGroup(group.Id, entryId);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        group.EntryIds.Should().Contain(entryId);
    }

    [Fact]
    public void AssignEntryToGroup_refuses_when_raw_membership_is_full_even_if_one_member_is_outside_composition()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var inPool = Enumerable.Range(0, 4).Select(_ => EntryId.New()).ToArray();
        var orphan = EntryId.New();
        stage.ReplaceAffectationAuthoring(inPool, _clock);
        stage.AssignEntryToGroup(group.Id, inPool[0]);
        stage.AssignEntryToGroup(group.Id, inPool[1]);
        stage.AssignEntryToGroup(group.Id, inPool[2]);
        stage.AssignEntryToGroup(group.Id, orphan);
        stage.SetPlacesPerGroup(4);

        var act = () => stage.AssignEntryToGroup(group.Id, inPool[3]);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.GroupFull);
        group.EntryIds.Should().NotContain(inPool[3]);
    }

    [Fact]
    public void RemoveDrawEntryFromGroup_in_Ready_is_allowed()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var entryId = EntryId.New();
        stage.AssignEntryToGroup(group.Id, entryId);
        stage.AssignEntryToGroup(group.Id, EntryId.New());
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);

        stage.RemoveDrawEntryFromGroup(group.Id, entryId);

        group.EntryIds.Should().NotContain(entryId);
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void RemoveDrawEntryFromGroup_in_Running_is_rejected()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var entryId = EntryId.New();
        stage.AssignEntryToGroup(group.Id, entryId);
        stage.AssignEntryToGroup(group.Id, EntryId.New());
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => stage.RemoveDrawEntryFromGroup(group.Id, entryId);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        group.EntryIds.Should().Contain(entryId);
        stage.Status.Should().Be(StageStatus.Running);
    }

    [Fact]
    public void RemoveDrawEntryFromGroup_entry_on_attached_match_is_rejected()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var matchday = stage.AddMatchday(1, _clock);
        var entryA = EntryId.New();
        var entryB = EntryId.New();
        stage.AssignEntryToGroup(group.Id, entryA);
        stage.AssignEntryToGroup(group.Id, entryB);
        var fixture = stage.AddFixture(matchday.Id, _clock);
        var match = Match.Create(_competitionId, stage.Id, entryA, entryB, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        var snapshots = new[] { new MatchParticipantSnapshot(match.Id, entryA, entryB) };
        stage.Prepare(_clock);

        var act = () => stage.RemoveDrawEntryFromGroup(group.Id, entryA, snapshots);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.MaterializedOpponentConflict);
        group.EntryIds.Should().Contain(entryA);
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void RemoveEntryFromGroup_missing_entry_is_rejected()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);

        // Act
        var act = () => stage.RemoveEntryFromGroup(group.Id, EntryId.New());

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.EntryNotFound);
    }

    [Fact]
    public void ArrangeGroups_permutation_in_Ready_is_rejected()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var a = stage.AddGroup("A", _clock);
        var b = stage.AddGroup("B", _clock);
        stage.AssignEntryToGroup(a.Id, EntryId.New());
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);

        var act = () => stage.ArrangeGroups([b.Id, a.Id]);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        stage.Groups.Select(g => g.Id).Should().Equal(a.Id, b.Id);
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void ArrangeGroups_identical_order_is_noop_in_Draft()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var a = stage.AddGroup("A", _clock);
        stage.AddGroup("B", _clock);
        stage.AssignEntryToGroup(a.Id, EntryId.New());
        stage.AddMatchday(1, _clock);
        var order = stage.Groups.Select(g => g.Id).ToList();
        stage.ClearDomainEvents();

        stage.ArrangeGroups(order);

        stage.Status.Should().Be(StageStatus.Draft);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ArrangeGroups_invalid_permutation_is_rejected()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var a = stage.AddGroup("A", _clock);

        // Act
        var act = () => stage.ArrangeGroups([a.Id, GroupId.New()]);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidOrder);
    }

    [Fact]
    public void GetGroup_unknown_id_is_rejected()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);

        // Act
        var act = () => stage.GetGroup(GroupId.New());

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.GroupNotFound);
    }

    [Fact]
    public void Events_order_Create_then_AddGroup_then_Prepare()
    {
        // Arrange & Act
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        stage.AssignEntryToGroup(group.Id, EntryId.New());
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
        var cup = Stage.Create(_competitionId, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        var r1 = cup.AddRound("QF", _clock);
        var r2 = cup.AddRound("SF", _clock);
        cup.Prepare(_clock);
        cup.Status.Should().Be(StageStatus.Ready);
        var reorderCup = () => cup.ArrangeRounds([r2.Id, r1.Id]);
        reorderCup.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        cup.Status.Should().Be(StageStatus.Ready);

        var league = Stage.Create(_competitionId, new StageName("League"), SampleRegulations.Standard(), _clock);
        var m1 = league.AddMatchday(1, _clock);
        var m2 = league.AddMatchday(2, _clock);
        league.Prepare(_clock);
        league.Status.Should().Be(StageStatus.Ready);
        var reorderLeague = () => league.ArrangeMatchdays([m2.Id, m1.Id]);
        reorderLeague.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        league.Status.Should().Be(StageStatus.Ready);
        league.Matchdays.Select(m => m.Id).Should().Equal(m1.Id, m2.Id);
    }

    private Stage StageWithTwoGroupsAndAMatchOnTheFirst(
        bool prepare,
        out Group quietGroup,
        out EntryId entryOnQuietGroup,
        out IReadOnlyList<MatchParticipantSnapshot> snapshots)
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var noisy = stage.AddGroup("A", _clock);
        quietGroup = stage.AddGroup("B", _clock);
        var matchday = stage.AddMatchday(1, _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        entryOnQuietGroup = EntryId.New();
        var partner = EntryId.New();
        stage.ReplaceAffectationAuthoring([home, away, entryOnQuietGroup, partner], _clock);
        stage.AssignEntryToGroup(noisy.Id, home);
        stage.AssignEntryToGroup(noisy.Id, away);
        stage.AssignEntryToGroup(quietGroup.Id, entryOnQuietGroup);
        stage.AssignEntryToGroup(quietGroup.Id, partner);
        var fixture = stage.AddFixture(matchday.Id, _clock);
        var match = Match.Create(_competitionId, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        snapshots = [new MatchParticipantSnapshot(match.Id, home, away)];
        if (prepare)
        {
            stage.Prepare(_clock);
        }

        return stage;
    }

    private Stage CreateReadyPoules()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        stage.AssignEntryToGroup(group.Id, EntryId.New());
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        return stage;
    }
}
