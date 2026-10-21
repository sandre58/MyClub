// -----------------------------------------------------------------------
// <copyright file="StageApplyResolvedGroupEntryTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

/// <summary>
/// Documents A1 membership invariant: <see cref="Stage.ApplyResolvedGroupEntry"/> mirrors
/// <see cref="Stage.AssignEntryToGroup"/> exclusivity (reject other-group; no-op same-group)
/// under resolution mutability (no Ready demotion).
/// </summary>
public sealed class StageApplyResolvedGroupEntryTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void ApplyResolvedGroupEntry_assigns_entry_to_vacant_group()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var entryId = EntryId.New();

        stage.ApplyResolvedGroupEntry(group.Id, entryId);

        group.EntryIds.Should().ContainSingle().Which.Should().Be(entryId);
    }

    [Fact]
    public void ApplyResolvedGroupEntry_same_group_twice_is_noop()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var entryId = EntryId.New();
        stage.ApplyResolvedGroupEntry(group.Id, entryId);

        stage.ApplyResolvedGroupEntry(group.Id, entryId);

        group.EntryIds.Should().ContainSingle().Which.Should().Be(entryId);
    }

    [Fact]
    public void ApplyResolvedGroupEntry_cross_group_duplicate_is_rejected_like_AssignEntryToGroup()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var groupA = stage.AddGroup("A", _clock);
        var groupB = stage.AddGroup("B", _clock);
        var entryId = EntryId.New();
        stage.ApplyResolvedGroupEntry(groupA.Id, entryId);

        var act = () => stage.ApplyResolvedGroupEntry(groupB.Id, entryId);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DuplicateEntry);
        groupA.EntryIds.Should().ContainSingle().Which.Should().Be(entryId);
        groupB.EntryIds.Should().BeEmpty();
    }

    [Fact]
    public void ApplyResolvedGroupEntry_does_not_demote_Ready()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        stage.AssignEntryToGroup(group.Id, EntryId.New());
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        stage.Status.Should().Be(StageStatus.Ready);

        stage.ApplyResolvedGroupEntry(group.Id, EntryId.New());

        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void ApplyResolvedGroupEntry_unknown_group_throws()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);

        var act = () => stage.ApplyResolvedGroupEntry(GroupId.New(), EntryId.New());

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.GroupNotFound);
    }
}
