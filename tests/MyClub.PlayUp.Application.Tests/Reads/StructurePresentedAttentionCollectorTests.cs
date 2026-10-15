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
            && item.Severity == NeedsAttentionAssembler.SeverityWarning
            && item.TargetId == poules.Id.Value.ToString());

        var readyHub = hub with { Status = StageStatus.Ready };
        var readyView = view with { Stages = [readyHub] };
        var readyItems = new List<NeedsAttentionItemDto>();
        StructurePresentedAttentionCollector.Collect(readyView, readyItems);
        readyItems.Should().Contain(item =>
            item.Source == CompetitionFlowAnalyzer.PopulationShortfall
            && item.Severity == NeedsAttentionAssembler.SeverityBlocking);
    }

    [Fact]
    public void ProjectPresentedIssueCodes_drops_ExpectedRank_when_AuthoredAward_present()
    {
        var codes = new[] { "AuthoredAwardUnreachable", "ExpectedRankUnreachable" };
        StructurePresentedAttentionCollector.ProjectPresentedIssueCodes(codes)
            .Should().Equal("AuthoredAwardUnreachable");
    }

    [Fact]
    public void ProjectPresentedStageIssueCodes_drops_SlotFeedMissing_when_draw_awaiting_apply()
    {
        var competition = CreateCompetition.Execute("Draw awaiting", _clock);
        for (var i = 0; i < 4; i++)
        {
            competition.AddEntry(TeamId.New(), $"Team {i + 1}", _clock);
        }

        var groups = Stage.Create(
            competition.Id,
            new StageName("Groupes"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        competition.AddStage(groups.Id, _clock);
        groups.AddGroup("A", _clock);
        groups.SetPlacesPerGroup(4);
        groups.AddMatchday(1, _clock);
        groups.ReplaceDrawRules(
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        groups.CreateDraw(DrawResolutionKind.Group, _clock);

        var view = StructureViewAssembler.Assemble(competition, [groups]);
        var hub = view.Stages.Single();
        hub.DrawExecutionBadge.Should().Be(StructureDrawExecutionBadge.InProgress);

        var presented = StructurePresentedAttentionCollector.ProjectPresentedStageIssueCodes(
            [CompetitionFlowAnalyzer.SlotFeedMissing],
            hub);

        presented.Should().NotContain(CompetitionFlowAnalyzer.SlotFeedMissing);
    }

    [Fact]
    public void ProjectPresentedStageIssueCodes_keeps_SlotFeedMissing_on_stage_without_awaiting_draw()
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
        var koHub = view.Stages.Single(s => s.Name == "KO");

        var presented = StructurePresentedAttentionCollector.ProjectPresentedStageIssueCodes(
            [CompetitionFlowAnalyzer.SlotFeedMissing],
            koHub);

        presented.Should().ContainSingle(CompetitionFlowAnalyzer.SlotFeedMissing);
    }

    [Fact]
    public void Assemble_omits_SlotFeedMissing_when_draw_awaiting_on_groups()
    {
        var competition = CreateCompetition.Execute("Draw awaiting attention", _clock);
        for (var i = 0; i < 4; i++)
        {
            competition.AddEntry(TeamId.New(), $"Team {i + 1}", _clock);
        }

        var groups = Stage.Create(
            competition.Id,
            new StageName("Groupes"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        competition.AddStage(groups.Id, _clock);
        groups.AddGroup("A", _clock);
        groups.SetPlacesPerGroup(4);
        groups.AddMatchday(1, _clock);
        groups.ReplaceDrawRules(
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        groups.CreateDraw(DrawResolutionKind.Group, _clock);

        var attention = NeedsAttentionAssembler.Assemble(
            competition,
            [groups],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        attention.Items.Should().NotContain(item =>
            item.Source == CompetitionFlowAnalyzer.SlotFeedMissing);
    }

    [Fact]
    public void Collect_emits_NonRootUnfed_as_warning()
    {
        var competition = CreateCompetition.Execute("Unfed", _clock);
        var root = Stage.Create(
            competition.Id,
            new StageName("Root"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        var leaf = Stage.Create(
            competition.Id,
            new StageName("Leaf"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        competition.AddStage(root.Id, _clock);
        competition.AddStage(leaf.Id, _clock);
        root.AddGroup("A", _clock);
        root.AddMatchday(1, _clock);
        leaf.AddRound("F", _clock);

        var view = StructureViewAssembler.Assemble(competition, [root, leaf]);
        var items = new List<NeedsAttentionItemDto>();
        StructurePresentedAttentionCollector.Collect(view, items);

        items.Should().ContainSingle(item =>
            item.Source == CompetitionFlowAnalyzer.NonRootUnfed
            && item.Severity == NeedsAttentionAssembler.SeverityWarning
            && item.TargetId == leaf.Id.Value.ToString());
    }

    [Fact]
    public void Collect_emits_SlotFeedMissing_as_blocking_when_not_awaiting_draw()
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
            && item.Severity == NeedsAttentionAssembler.SeverityBlocking
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
