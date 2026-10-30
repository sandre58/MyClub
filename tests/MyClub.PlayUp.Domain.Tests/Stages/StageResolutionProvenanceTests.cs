// -----------------------------------------------------------------------
// <copyright file="StageResolutionProvenanceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class StageResolutionProvenanceTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void ApplyResolvedEntry_with_provenance_persists_on_slot()
    {
        var stage = CreateCupWithSlots("A");
        var entryId = EntryId.New();
        var provenance = ResolutionProvenance.Directe("A", entryId);

        stage.ApplyResolvedEntry("A", entryId, _clock, provenance);

        var slot = stage.FindSlot("A")!;
        slot.EntryId.Should().Be(entryId);
        slot.ResolutionProvenance.Should().BeEquivalentTo(provenance);
    }

    [Fact]
    public void AddResolvedPopulationEntry_with_provenance_persists_on_composition_row()
    {
        var stage = CreateCupWithSlots("A");
        var entryId = EntryId.New();
        var provenance = ResolutionProvenance.NonVerifiee();

        stage.AddResolvedPopulationEntry(entryId, _clock, provenance);

        stage.CompositionEntries.Should().ContainSingle()
            .Which.ResolutionProvenance.Should().BeEquivalentTo(provenance);
    }

    [Fact]
    public void ApplyResolvedGroupEntry_with_provenance_persists_group_witness()
    {
        var stage = CreateGroupsStage();
        var group = stage.AddGroup("A", _clock);
        var entryId = EntryId.New();
        stage.AssignEntryToGroup(group.Id, entryId);
        var provenance = ResolutionProvenance.NonVerifiee();

        stage.ApplyResolvedGroupEntry(group.Id, entryId, provenance);

        stage.GroupResolutionProvenances.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new GroupResolutionProvenance(group.Id, entryId, provenance));
    }

    [Fact]
    public void WriteSlotResolutionProvenance_does_not_change_occupant()
    {
        var stage = CreateCupWithSlots("A");
        var entryId = EntryId.New();
        stage.ApplyResolvedEntry("A", entryId, _clock, ResolutionProvenance.NonVerifiee());
        stage.ClearDomainEvents();

        stage.WriteSlotResolutionProvenance("A", ResolutionProvenance.Directe("A", entryId));

        stage.FindSlot("A")!.EntryId.Should().Be(entryId);
        stage.FindSlot("A")!.ResolutionProvenance!.Kind.Should().Be(ResolutionProvenanceKind.Directe);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void WriteCompositionResolutionProvenance_does_not_change_membership()
    {
        var stage = CreateCupWithSlots("A");
        var entryId = EntryId.New();
        stage.AddResolvedPopulationEntry(entryId, _clock, ResolutionProvenance.NonVerifiee());

        stage.WriteCompositionResolutionProvenance(entryId, ResolutionProvenance.Directe("A", entryId));

        stage.CompositionEntries.Should().ContainSingle().Which.EntryId.Should().Be(entryId);
        stage.CompositionEntries.Single().ResolutionProvenance!.Kind.Should().Be(ResolutionProvenanceKind.Directe);
    }

    private static Stage CreateCupWithSlots(params string[] slotKeys)
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            new FakeClock(DateTimeOffset.UtcNow));
        foreach (var key in slotKeys)
        {
            stage.AddSlot(key);
        }

        return stage;
    }

    private Stage CreateGroupsStage()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Groups"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddGroup("A", _clock);
        return stage;
    }
}
