// -----------------------------------------------------------------------
// <copyright file="GroupReplaceBatchTests.cs" company="Stéphane ANDRE">
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
/// Seat replacement keeps the index and purges the occupant's provenance.
/// A lot is refused as a whole when one entry does not fit.
/// </summary>
public sealed class GroupReplaceBatchTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Replace_keeps_the_seat_index_and_purges_occupant_provenance()
    {
        var stage = Groups(4);
        var ids = Composition(stage, 4);
        var group = stage.Groups[0];
        stage.AssignEntryToGroup(group.Id, ids[0]);
        stage.AssignEntryToGroup(group.Id, ids[1]);
        stage.AssignEntryToGroup(group.Id, ids[2]);
        stage.WriteGroupResolutionProvenance(
            group.Id,
            ids[1],
            new ResolutionProvenance(ResolutionProvenanceKind.Tirage, "draw", "fp"));

        stage.ReplaceManualEntryInGroup(group.Id, ids[1], ids[3]);

        group.EntryIds.Should().Equal(ids[0], ids[3], ids[2]);
        stage.GroupResolutionProvenances.Should().NotContain(row =>
            row.GroupId.Equals(group.Id) && row.EntryId.Equals(ids[1]));
        stage.GroupResolutionProvenances.Should().NotContain(row => row.EntryId.Equals(ids[3]));
    }

    [Fact]
    public void Replace_with_the_same_team_has_no_effect()
    {
        var stage = Groups(4);
        var ids = Composition(stage, 2);
        var group = stage.Groups[0];
        stage.AssignEntryToGroup(group.Id, ids[0]);
        stage.WriteGroupResolutionProvenance(
            group.Id,
            ids[0],
            new ResolutionProvenance(ResolutionProvenanceKind.Tirage, "draw", "fp"));

        stage.ReplaceManualEntryInGroup(group.Id, ids[0], ids[0]);

        group.EntryIds.Should().Equal(ids[0]);
        stage.GroupResolutionProvenances.Should().ContainSingle(row => row.EntryId.Equals(ids[0]));
    }

    [Fact]
    public void Batch_refuses_the_whole_list_when_one_entry_does_not_fit()
    {
        var stage = Groups(2);
        var ids = Composition(stage, 4);
        var group = stage.Groups[0];
        stage.AssignEntryToGroup(group.Id, ids[0]);

        var act = () => stage.AssignManualEntriesToGroup(group.Id, [ids[1], ids[2]]);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.GroupFull);
        group.EntryIds.Should().Equal(ids[0]);
    }

    [Fact]
    public void Batch_refuses_when_places_per_group_is_absent()
    {
        var stage = Groups(2);
        var ids = Composition(stage, 2);
        stage.SetPlacesPerGroup(null);

        var act = () => stage.AssignManualEntriesToGroup(stage.Groups[0].Id, [ids[0]]);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidConfiguration);
        stage.Groups[0].EntryIds.Should().BeEmpty();
    }

    private Stage Groups(int places)
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Poules"),
            SampleRegulations.Standard(),
            _clock);
        _ = stage.AddGroup("A", _clock);
        _ = stage.AddGroup("B", _clock);
        stage.SetPlacesPerGroup(places);
        return stage;
    }

    private EntryId[] Composition(Stage stage, int count)
    {
        var ids = Enumerable.Range(0, count).Select(_ => EntryId.New()).ToArray();
        stage.ReplaceAffectationAuthoring(ids, _clock);
        return ids;
    }
}
