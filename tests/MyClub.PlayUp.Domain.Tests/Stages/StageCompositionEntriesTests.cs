// -----------------------------------------------------------------------
// <copyright file="StageCompositionEntriesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
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
}
