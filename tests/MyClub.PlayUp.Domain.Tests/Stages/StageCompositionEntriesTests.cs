// -----------------------------------------------------------------------
// <copyright file="StageCompositionEntriesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Stages.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class StageCompositionEntriesTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 12, 12, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void ReplaceAffectationAuthoring_stores_authoring_and_syncs_composition()
    {
        var stage = Stage.Create(_competitionId, new StageName("Root"), SampleRegulations.Standard(), _clock);
        stage.ClearDomainEvents();
        var a = EntryId.New();
        var b = EntryId.New();

        stage.ReplaceAffectationAuthoring([a, b], _clock);

        stage.AffectationAuthoring.Select(e => e.EntryId).Should().Equal(a, b);
        stage.CompositionEntries.Select(e => e.EntryId).Should().Equal(a, b);
        stage.DomainEvents.Should().Contain(e => e is StageAffectationAuthoringReplaced);
        stage.DomainEvents.Should().Contain(e => e is StageCompositionEntriesReplaced);
    }

    [Fact]
    public void ReplaceAffectationAuthoring_rejects_duplicates()
    {
        var stage = Stage.Create(_competitionId, new StageName("Root"), SampleRegulations.Standard(), _clock);
        var a = EntryId.New();

        var act = () => stage.ReplaceAffectationAuthoring([a, a], _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DuplicateEntry);
    }

    [Fact]
    public void ReplaceAffectationAuthoring_allows_partial_and_clear_authoring()
    {
        var stage = Stage.Create(_competitionId, new StageName("Root"), SampleRegulations.Standard(), _clock);
        var a = EntryId.New();
        stage.ReplaceAffectationAuthoring([a], _clock);
        stage.ClearDomainEvents();

        stage.ReplaceAffectationAuthoring([], _clock);

        stage.AffectationAuthoring.Should().BeEmpty();
        stage.CompositionEntries.Should().BeEmpty();
        stage.DomainEvents.Should().Contain(e => e is StageAffectationAuthoringReplaced);
        stage.DomainEvents.Should().Contain(e => e is StageCompositionEntriesReplaced);
    }

    [Fact]
    public void ReplaceAffectationAuthoring_preserves_apply_resolved_entries()
    {
        var stage = Stage.Create(_competitionId, new StageName("Root"), SampleRegulations.Standard(), _clock);
        var affectation = EntryId.New();
        var resolved = EntryId.New();
        stage.ReplaceAffectationAuthoring([affectation], _clock);
        stage.AddResolvedPopulationEntry(resolved, _clock);
        stage.ClearDomainEvents();

        stage.ReplaceAffectationAuthoring([affectation], _clock);

        stage.AffectationAuthoring.Select(e => e.EntryId).Should().Equal(affectation);
        stage.CompositionEntries.Select(e => e.EntryId).Should().BeEquivalentTo([affectation, resolved]);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ReplaceAffectationAuthoring_removing_authoring_keeps_apply_only_membership()
    {
        var stage = Stage.Create(_competitionId, new StageName("Root"), SampleRegulations.Standard(), _clock);
        var affectation = EntryId.New();
        var resolved = EntryId.New();
        stage.ReplaceAffectationAuthoring([affectation], _clock);
        stage.AddResolvedPopulationEntry(resolved, _clock);
        stage.ClearDomainEvents();

        stage.ReplaceAffectationAuthoring([], _clock);

        stage.AffectationAuthoring.Should().BeEmpty();
        stage.CompositionEntries.Select(e => e.EntryId).Should().Equal(resolved);
        stage.DomainEvents.Should().Contain(e => e is StageAffectationAuthoringReplaced);
        stage.DomainEvents.Should().Contain(e => e is StageCompositionEntriesReplaced);
    }

    [Fact]
    public void ReplaceAffectationAuthoring_never_wipes_composition_wholesale()
    {
        var stage = Stage.Create(_competitionId, new StageName("Root"), SampleRegulations.Standard(), _clock);
        var a = EntryId.New();
        var b = EntryId.New();
        var resolved = EntryId.New();
        stage.ReplaceAffectationAuthoring([a, b], _clock);
        stage.AddResolvedPopulationEntry(resolved, _clock);

        stage.ReplaceAffectationAuthoring([a], _clock);

        stage.AffectationAuthoring.Select(e => e.EntryId).Should().Equal(a);
        stage.CompositionEntries.Select(e => e.EntryId).Should().BeEquivalentTo([a, resolved]);
        stage.CompositionEntries.Should().NotContain(e => e.EntryId.Equals(b));
    }

    [Fact]
    public void ClearCompositionEntries_clears_authoring_membership_and_form_resolutions()
    {
        var stage = Stage.Create(_competitionId, new StageName("Root"), SampleRegulations.Standard(), _clock);
        var a = EntryId.New();
        var resolved = EntryId.New();
        stage.ReplaceAffectationAuthoring([a], _clock);
        stage.AddResolvedPopulationEntry(resolved, _clock);
        stage.ClearDomainEvents();

        stage.ClearCompositionEntries(_clock);

        stage.AffectationAuthoring.Should().BeEmpty();
        stage.CompositionEntries.Should().BeEmpty();
        stage.DomainEvents.Should().Contain(e => e is StageAffectationAuthoringReplaced);
        stage.DomainEvents.Should().Contain(e => e is StageCompositionEntriesReplaced);
    }

    [Fact]
    public void RemoveCompositionEntryIfPresent_noops_when_absent()
    {
        var stage = Stage.Create(_competitionId, new StageName("Root"), SampleRegulations.Standard(), _clock);
        stage.ClearDomainEvents();

        stage.RemoveCompositionEntryIfPresent(EntryId.New(), _clock);

        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RemoveCompositionEntryIfPresent_removes_from_authoring_and_membership()
    {
        var stage = Stage.Create(_competitionId, new StageName("Root"), SampleRegulations.Standard(), _clock);
        var a = EntryId.New();
        stage.ReplaceAffectationAuthoring([a], _clock);
        stage.ClearDomainEvents();

        stage.RemoveCompositionEntryIfPresent(a, _clock);

        stage.AffectationAuthoring.Should().BeEmpty();
        stage.CompositionEntries.Should().BeEmpty();
        stage.DomainEvents.Should().Contain(e => e is StageAffectationAuthoringReplaced);
        stage.DomainEvents.Should().Contain(e => e is StageCompositionEntriesReplaced);
    }

    [Fact]
    public void Withdrawal_without_a_match_clears_group_membership_in_the_same_operation()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var staying = EntryId.New();
        var leaving = EntryId.New();
        stage.ReplaceAffectationAuthoring([staying, leaving], _clock);
        stage.AssignEntryToGroup(group.Id, leaving);

        stage.ReplaceAffectationAuthoring([staying], _clock);

        group.EntryIds.Should().NotContain(leaving);
        stage.AffectationAuthoring.Select(entry => entry.EntryId).Should().Equal(staying);
        stage.CompositionEntries.Select(entry => entry.EntryId).Should().BeEquivalentTo([staying]);
    }

    [Fact]
    public void Withdrawal_with_an_attached_match_leaves_affectation_and_group_unchanged()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var matchday = stage.AddMatchday(1, _clock);
        var staying = EntryId.New();
        var leaving = EntryId.New();
        stage.ReplaceAffectationAuthoring([staying, leaving], _clock);
        stage.AssignEntryToGroup(group.Id, staying);
        stage.AssignEntryToGroup(group.Id, leaving);
        var fixture = stage.AddFixture(matchday.Id, _clock);
        var match = Match.Create(_competitionId, stage.Id, staying, leaving, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        var snapshots = new[] { new MatchParticipantSnapshot(match.Id, staying, leaving) };

        var act = () => stage.ReplaceAffectationAuthoring([staying], _clock, snapshots);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.CompositionWithdrawalGroupMembershipBlocked);
        group.EntryIds.Should().Contain(leaving);
        stage.AffectationAuthoring.Select(entry => entry.EntryId).Should().Equal(staying, leaving);
    }

    [Fact]
    public void Withdrawal_of_a_qualification_placement_is_refused_with_the_same_code()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var staying = EntryId.New();
        var leaving = EntryId.New();
        stage.ReplaceAffectationAuthoring([staying, leaving], _clock);
        stage.AssignEntryToGroup(group.Id, leaving);
        stage.WriteGroupResolutionProvenance(
            group.Id,
            leaving,
            new ResolutionProvenance(ResolutionProvenanceKind.Qualification, "Q|path", "fp"));

        var act = () => stage.ReplaceAffectationAuthoring([staying], _clock);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.CompositionWithdrawalGroupMembershipBlocked);
        group.EntryIds.Should().Contain(leaving);
        stage.AffectationAuthoring.Select(entry => entry.EntryId).Should().Equal(staying, leaving);
    }

    [Fact]
    public void RemoveCompositionEntryIfPresent_clears_a_qualification_sealed_group_membership()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var entryId = EntryId.New();
        stage.ReplaceAffectationAuthoring([entryId], _clock);
        stage.AssignEntryToGroup(group.Id, entryId);
        stage.WriteGroupResolutionProvenance(
            group.Id,
            entryId,
            new ResolutionProvenance(ResolutionProvenanceKind.Qualification, "Q|path", "fp"));

        stage.RemoveCompositionEntryIfPresent(entryId, _clock);

        group.EntryIds.Should().BeEmpty();
        stage.CompositionEntries.Should().BeEmpty();
        stage.AffectationAuthoring.Should().BeEmpty();
    }

    [Fact]
    public void RemoveCompositionEntryIfPresent_clears_a_group_member_that_is_not_in_the_composition()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var entryId = EntryId.New();
        stage.AssignEntryToGroup(group.Id, entryId);

        stage.RemoveCompositionEntryIfPresent(entryId, _clock);

        group.EntryIds.Should().BeEmpty();
        stage.DomainEvents.Should().NotContain(e => e is StageCompositionEntriesReplaced);
    }
}
