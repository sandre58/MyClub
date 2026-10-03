// -----------------------------------------------------------------------
// <copyright file="MaterializedOpponentCoherenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class MaterializedOpponentCoherenceTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Cup_slot_mutation_before_materialize_is_allowed()
    {
        var (stage, entryA, entryB, _) = CreateMaterializedCupPair(attachMatches: false);

        stage.ApplyResolvedEntry("S1", entryA, _clock);
        stage.ApplyResolvedEntry("S2", entryB, _clock);

        stage.FindSlot("S1")!.EntryId.Should().Be(entryA);
        stage.FindSlot("S2")!.EntryId.Should().Be(entryB);
    }

    [Fact]
    public void Cup_slot_change_after_materialize_is_refused()
    {
        var (stage, entryA, entryB, snapshots) = CreateMaterializedCupPair(attachMatches: true);
        var other = EntryId.New();

        var act = () => stage.ApplyResolvedEntry("S1", other, _clock, snapshots);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MaterializedOpponentConflict);
        stage.FindSlot("S1")!.EntryId.Should().Be(entryA);
        stage.FindSlot("S2")!.EntryId.Should().Be(entryB);
    }

    [Fact]
    public void Cup_move_from_materialized_pair_is_refused_on_final_pair_state()
    {
        var (stage, entryA, entryB, snapshots) = CreateMaterializedCupPair(attachMatches: true);
        stage.AddSlot("S3");
        stage.AddSlot("S4");

        var act = () => stage.ApplyResolvedEntry("S3", entryA, _clock, snapshots);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MaterializedOpponentConflict);
        stage.FindSlot("S1")!.EntryId.Should().Be(entryA);
        stage.FindSlot("S3")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Cup_same_occupant_after_materialize_is_noop()
    {
        var (stage, entryA, _, snapshots) = CreateMaterializedCupPair(attachMatches: true);

        stage.ApplyResolvedEntry("S1", entryA, _clock, snapshots);

        stage.FindSlot("S1")!.EntryId.Should().Be(entryA);
    }

    [Fact]
    public void Cup_clear_after_materialize_is_refused()
    {
        var (stage, entryA, _, snapshots) = CreateMaterializedCupPair(attachMatches: true);

        var act = () => stage.ClearResolvedEntry("S1", _clock, snapshots);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MaterializedOpponentConflict);
        stage.FindSlot("S1")!.EntryId.Should().Be(entryA);
    }

    [Fact]
    public void Groups_remove_entry_on_attached_match_is_refused()
    {
        var (stage, entryA, entryB, groupId, snapshots) = CreateMaterializedGroupsPair();

        var act = () => stage.RemoveEntryFromGroup(groupId, entryA, snapshots);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MaterializedOpponentConflict);
        stage.FindGroup(groupId)!.EntryIds.Should().Contain(entryA);
        stage.FindGroup(groupId)!.EntryIds.Should().Contain(entryB);
    }

    [Fact]
    public void Groups_add_entry_after_materialize_is_allowed()
    {
        var (stage, _, _, groupId, snapshots) = CreateMaterializedGroupsPair();
        var entryC = EntryId.New();

        stage.AssignEntryToGroup(groupId, entryC);

        stage.FindGroup(groupId)!.EntryIds.Should().Contain(entryC);

        var act = () => stage.RemoveEntryFromGroup(groupId, entryC, snapshots);
        act.Should().NotThrow();
    }

    [Fact]
    public void Groups_remove_entry_absent_from_matches_is_allowed()
    {
        var (stage, entryA, entryB, groupId, snapshots) = CreateMaterializedGroupsPair();
        var entryC = EntryId.New();
        stage.AssignEntryToGroup(groupId, entryC);

        stage.RemoveEntryFromGroup(groupId, entryC, snapshots);

        stage.FindGroup(groupId)!.EntryIds.Should().NotContain(entryC);
        stage.FindGroup(groupId)!.EntryIds.Should().Contain(entryA);
        stage.FindGroup(groupId)!.EntryIds.Should().Contain(entryB);
    }

    [Fact]
    public void Groups_move_entry_on_attached_match_is_refused()
    {
        var (stage, entryA, entryB, groupId, snapshots) = CreateMaterializedGroupsPair();
        var otherGroup = stage.AddGroup("B", _clock);

        var act = () => stage.RemoveEntryFromGroup(groupId, entryA, snapshots);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MaterializedOpponentConflict);
        stage.FindGroup(groupId)!.EntryIds.Should().Contain(entryA);
        stage.FindGroup(groupId)!.EntryIds.Should().Contain(entryB);
        otherGroup.EntryIds.Should().BeEmpty();
    }

    private (Stage Stage, EntryId EntryA, EntryId EntryB, IReadOnlyList<MatchParticipantSnapshot> Snapshots)
        CreateMaterializedCupPair(bool attachMatches)
    {
        var competitionId = CompetitionId.New();
        var stage = Stage.Create(
            competitionId,
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        stage.SeedEntryRoundBracketPairs();
        var round = stage.AddRound("Tour", _clock);

        var entryA = EntryId.New();
        var entryB = EntryId.New();
        stage.ApplyResolvedEntry("S1", entryA, _clock);
        stage.ApplyResolvedEntry("S2", entryB, _clock);

        if (!attachMatches)
        {
            return (stage, entryA, entryB, []);
        }

        var fixture = stage.AddFixture(round.Id, _clock, "S1", "S2", "P1");
        var match = Match.Create(competitionId, stage.Id, entryA, entryB, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        var snapshots = new[] { new MatchParticipantSnapshot(match.Id, entryA, entryB) };
        return (stage, entryA, entryB, snapshots);
    }

    private (Stage Stage, EntryId EntryA, EntryId EntryB, GroupId GroupId, IReadOnlyList<MatchParticipantSnapshot> Snapshots)
        CreateMaterializedGroupsPair()
    {
        var competitionId = CompetitionId.New();
        var stage = Stage.Create(
            competitionId,
            new StageName("Groups"),
            SampleRegulations.Standard(),
            _clock);
        var group = stage.AddGroup("A", _clock);
        var matchday = stage.AddMatchday(1, _clock);
        var entryA = EntryId.New();
        var entryB = EntryId.New();
        stage.AssignEntryToGroup(group.Id, entryA);
        stage.AssignEntryToGroup(group.Id, entryB);

        var fixture = stage.AddFixture(matchday.Id, _clock);
        var match = Match.Create(competitionId, stage.Id, entryA, entryB, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        var snapshots = new[] { new MatchParticipantSnapshot(match.Id, entryA, entryB) };
        return (stage, entryA, entryB, group.Id, snapshots);
    }
}
