// -----------------------------------------------------------------------
// <copyright file="StructurePresentedAttentionCollectorTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructurePresentedAttentionCollectorTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Collect_synthesizes_PopulationShortfall_when_occupancy_below_Places_N()
    {
        var competition = CreateCompetition.Execute("U1 shortfall", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);

        var poules = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        competition.AddStage(poules.Id, _clock);
        poules.AddGroup("A", _clock);
        poules.AddGroup("B", _clock);
        poules.SetPlacesPerGroup(4);
        poules.AddMatchday(1, _clock);

        var view = StructureViewAssembler.Assemble(competition, [poules]);
        var hub = view.Stages.Single();
        hub.CompositionCapacity.Should().Be(8);

        StructurePopulationVolume.ShouldSynthesizePopulationShortfall(view, hub, [])
            .Should().BeTrue();

        var items = new List<NeedsAttentionItemDto>();
        StructurePresentedAttentionCollector.Collect(view, items);

        items.Should().Contain(item =>
            item.Source == CompetitionFlowAnalyzer.PopulationShortfall
            && item.Severity == NeedsAttentionAssembler.SeverityBlocking
            && item.TargetId == poules.Id.Value.ToString());
    }

    [Fact]
    public void ProjectPresentedIssueCodes_drops_ExpectedRank_when_AuthoredAward_present()
    {
        var codes = new[] { "AuthoredAwardUnreachable", "ExpectedRankUnreachable" };
        StructurePresentedAttentionCollector.ProjectPresentedIssueCodes(codes)
            .Should().Equal("AuthoredAwardUnreachable");
    }

    [Fact]
    public void Collect_emits_SlotFeedMissing_as_warning_when_not_awaiting_draw()
    {
        var competition = CreateCompetition.Execute("Feeds", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);

        var source = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        competition.AddStage(source.Id, _clock);
        source.AddGroup("A", _clock);
        source.AddMatchday(1, _clock);

        var target = Stage.Create(
            competition.Id,
            new StageName("KO"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        competition.AddStage(target.Id, _clock);
        target.AddRound("QF", _clock);
        target.AddSlot("SF1-A");
        target.AddSlot("SF1-B");

        var view = StructureViewAssembler.Assemble(competition, [source, target]);
        var items = new List<NeedsAttentionItemDto>();
        StructurePresentedAttentionCollector.Collect(view, items);

        items.Should().ContainSingle(item =>
            item.Source == CompetitionFlowAnalyzer.SlotFeedMissing
            && item.Severity == NeedsAttentionAssembler.SeverityWarning
            && item.TargetType == "Stage"
            && item.TargetId == target.Id.Value.ToString());
    }

    [Fact]
    public void Assemble_includes_structure_diagnostics_with_runtime_T5()
    {
        var competition = CreateCompetition.Execute("Feeds", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);

        var source = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        competition.AddStage(source.Id, _clock);
        source.AddGroup("A", _clock);
        source.AddMatchday(1, _clock);

        var target = Stage.Create(
            competition.Id,
            new StageName("KO"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        competition.AddStage(target.Id, _clock);
        target.AddRound("QF", _clock);
        target.AddSlot("SF1-A");
        target.AddSlot("SF1-B");

        var attention = NeedsAttentionAssembler.Assemble(
            competition,
            [source, target],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        attention.Items.Should().Contain(item =>
            item.Source == CompetitionFlowAnalyzer.SlotFeedMissing);
    }
}
