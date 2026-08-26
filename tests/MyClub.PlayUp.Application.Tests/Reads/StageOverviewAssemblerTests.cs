// -----------------------------------------------------------------------
// <copyright file="StageOverviewAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StageOverviewAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 22, 10, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_maps_rounds_fixtures_empty_slots_and_draft_draw()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        stage.AddSlot("QF1-A", _clock);
        stage.AddSlot("QF1-B", _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Pairing, _clock);

        var overview = StageOverviewAssembler.Assemble(stage, competition);

        overview.Id.Should().Be(stage.Id.Value);
        overview.CompetitionId.Should().Be(competition.Id.Value);
        overview.Name.Should().Be("QF");
        overview.Rounds.Should().ContainSingle();
        overview.Rounds[0].Fixtures.Should().ContainSingle();
        overview.Rounds[0].Fixtures[0].Id.Should().Be(fixture.Id.Value);
        overview.Rounds[0].Fixtures[0].Attachments.Should().BeEmpty();
        overview.Slots.Should().HaveCount(2);
        overview.Slots.Should().OnlyContain(slot =>
            slot.EntryId == null && slot.DisplayName == null && !slot.CoveredByCompleteFixture);
        overview.Draws.Should().ContainSingle();
        overview.Draws[0].Id.Should().Be(draw.Id.Value);
        overview.Draws[0].Kind.Should().Be(DrawResolutionKind.Pairing);
        overview.Draws[0].Status.Should().Be(DrawStatus.Draft);
        overview.Draws[0].ResolutionState.Should().Be(DrawResolutionState.NotResolved);
        overview.Draws[0].Pairings.Should().BeEmpty();
        home.Id.Should().NotBe(away.Id);
    }

    [Fact]
    public void Assemble_maps_resolved_pairing_draw_and_populated_slots_with_display_names()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var away = competition.AddEntry(TeamId.New(), "Beta", _clock);
        var stage = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("SF1-A", _clock);
        stage.AddSlot("SF1-B", _clock);
        stage.AssignEntryToSlot("SF1-A", home.Id, _clock);

        var draw = stage.CreateDraw(DrawResolutionKind.Pairing, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForPairing([home.Id, away.Id]), _clock);
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedPairings([new PairingDrawResult(home.Id, away.Id)]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);

        var overview = StageOverviewAssembler.Assemble(stage, competition);

        overview.Slots.Single(slot => slot.SlotKey == "SF1-A").EntryId.Should().Be(home.Id.Value);
        overview.Slots.Single(slot => slot.SlotKey == "SF1-A").DisplayName.Should().Be("Alpha");
        overview.Slots.Single(slot => slot.SlotKey == "SF1-B").EntryId.Should().BeNull();
        overview.Draws[0].Status.Should().Be(DrawStatus.Published);
        overview.Draws[0].ResolutionState.Should().Be(DrawResolutionState.Resolved);
        overview.Draws[0].Pairings.Should().ContainSingle();
        overview.Draws[0].Pairings[0].EntryADisplayName.Should().Be("Alpha");
        overview.Draws[0].Pairings[0].EntryBDisplayName.Should().Be("Beta");
    }

    [Fact]
    public void Assemble_maps_slot_draw_placements()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var entry = competition.AddEntry(TeamId.New(), "Seeded", _clock);
        var stage = Stage.Create(competition.Id, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("A", _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry.Id]), _clock);
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry.Id, "A")]),
            _clock);

        var overview = StageOverviewAssembler.Assemble(stage, competition);

        overview.Draws[0].SlotPlacements.Should().ContainSingle();
        overview.Draws[0].SlotPlacements[0].SlotKey.Should().Be("A");
        overview.Draws[0].SlotPlacements[0].DisplayName.Should().Be("Seeded");
        overview.Draws[0].Pairings.Should().BeEmpty();
    }

    [Fact]
    public void Assemble_marks_slots_covered_by_complete_fixture()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var a = competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var b = competition.AddEntry(TeamId.New(), "Beta", _clock);
        var c = competition.AddEntry(TeamId.New(), "Gamma", _clock);
        var d = competition.AddEntry(TeamId.New(), "Delta", _clock);
        var stage = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("SF", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("SF1-A", _clock);
        stage.AddSlot("SF1-B", _clock);
        stage.AddSlot("SF2-A", _clock);
        stage.AddSlot("SF2-B", _clock);
        stage.ApplyResolvedEntry("SF1-A", a.Id, _clock);
        stage.ApplyResolvedEntry("SF1-B", b.Id, _clock);
        stage.ApplyResolvedEntry("SF2-A", c.Id, _clock);
        stage.ApplyResolvedEntry("SF2-B", d.Id, _clock);

        MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            [new CupSlotPair("SF1-A", "SF1-B")],
            [],
            _clock);

        var overview = StageOverviewAssembler.Assemble(stage, competition);

        overview.Slots.Single(slot => slot.SlotKey == "SF1-A").CoveredByCompleteFixture.Should().BeTrue();
        overview.Slots.Single(slot => slot.SlotKey == "SF1-B").CoveredByCompleteFixture.Should().BeTrue();
        overview.Slots.Single(slot => slot.SlotKey == "SF2-A").CoveredByCompleteFixture.Should().BeFalse();
        overview.Slots.Single(slot => slot.SlotKey == "SF2-B").CoveredByCompleteFixture.Should().BeFalse();
    }
}
