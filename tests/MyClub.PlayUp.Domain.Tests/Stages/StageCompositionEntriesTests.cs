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
    public void ReplaceCompositionEntries_stores_distinct_set_and_raises_event()
    {
        var stage = Stage.Create(_competitionId, new StageName("Root"), SampleRegulations.Standard(), _clock);
        stage.ClearDomainEvents();
        var a = EntryId.New();
        var b = EntryId.New();

        stage.ReplaceCompositionEntries([a, b], _clock);

        stage.CompositionEntries.Select(e => e.EntryId).Should().Equal(a, b);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageCompositionEntriesReplaced>();
    }

    [Fact]
    public void ReplaceCompositionEntries_rejects_duplicates()
    {
        var stage = Stage.Create(_competitionId, new StageName("Root"), SampleRegulations.Standard(), _clock);
        var a = EntryId.New();

        var act = () => stage.ReplaceCompositionEntries([a, a], _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DuplicateEntry);
    }

    [Fact]
    public void ReplaceCompositionEntries_allows_partial_and_clear()
    {
        var stage = Stage.Create(_competitionId, new StageName("Root"), SampleRegulations.Standard(), _clock);
        var a = EntryId.New();
        stage.ReplaceCompositionEntries([a], _clock);
        stage.ClearDomainEvents();

        stage.ReplaceCompositionEntries([], _clock);

        stage.CompositionEntries.Should().BeEmpty();
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageCompositionEntriesReplaced>();
    }

    [Fact]
    public void RemoveCompositionEntryIfPresent_noops_when_absent()
    {
        var stage = Stage.Create(_competitionId, new StageName("Root"), SampleRegulations.Standard(), _clock);
        stage.ClearDomainEvents();

        stage.RemoveCompositionEntryIfPresent(EntryId.New(), _clock);

        stage.DomainEvents.Should().BeEmpty();
    }
}
