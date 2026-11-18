// -----------------------------------------------------------------------
// <copyright file="ResolutionReleasePrimitiveTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class ResolutionReleasePrimitiveTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void RemoveFormPathResolution_drops_the_witness_and_ignores_an_absent_one()
    {
        var stage = Stage.Create(CompetitionId.New(), new StageName("Suite"), SampleRegulations.Standard(), _clock);
        var entry = EntryId.New();
        stage.RecordFormPathResolution("Q|path", entry);

        stage.RemoveFormPathResolution("Q|path");
        stage.RemoveFormPathResolution("Q|missing");

        stage.FormPathResolutions.Should().BeEmpty();
    }

    [Fact]
    public void RemoveFormPathResolution_is_refused_when_the_phase_is_completed()
    {
        var stage = Stage.Create(CompetitionId.New(), new StageName("Suite"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        stage.RecordFormPathResolution("Q|path", EntryId.New());
        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.Complete(_clock);

        var act = () => stage.RemoveFormPathResolution("Q|path");

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
        stage.FormPathResolutions.Should().ContainSingle();
    }

    [Fact]
    public void ClearResolvedEntry_clears_the_provenance()
    {
        var stage = Stage.Create(CompetitionId.New(), new StageName("Suite"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("Champ");
        var entry = EntryId.New();
        var provenance = Provenance();
        stage.AddResolvedPopulationEntry(entry, _clock, provenance);
        stage.ApplyResolvedEntry("Champ", entry, _clock, provenance);

        stage.ClearResolvedEntry("Champ", _clock);

        var slot = stage.FindSlot("Champ");
        slot!.EntryId.Should().BeNull();
        slot.ResolutionProvenance.Should().BeNull();
    }

    [Fact]
    public void RemoveResolvedGroupEntry_clears_the_provenance()
    {
        var stage = Stage.Create(CompetitionId.New(), new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        var entry = EntryId.New();
        var provenance = Provenance();
        stage.AddResolvedPopulationEntry(entry, _clock, provenance);
        stage.ApplyResolvedGroupEntry(group.Id, entry, provenance);

        stage.RemoveResolvedGroupEntry(group.Id, entry);

        group.EntryIds.Should().NotContain(entry);
        stage.GroupResolutionProvenances.Should().BeEmpty();
    }

    private static ResolutionProvenance Provenance() =>
        new(ResolutionProvenanceKind.Qualification, "Q|path", "seed");
}
