// -----------------------------------------------------------------------
// <copyright file="CompetitionFlowAnalyzerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class CompetitionFlowAnalyzerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void AnalyzeTopology_detects_cycle_between_stages()
    {
        var competition = CreateCompetition.Execute("Cycle", _clock);
        var a = Stage.Create(
            competition.Id,
            new StageName("A"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        var b = Stage.Create(
            competition.Id,
            new StageName("B"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        competition.AddStage(a.Id, _clock);
        competition.AddStage(b.Id, _clock);
        a.AddGroup("A", _clock);
        a.AddMatchday(1, _clock);
        b.AddGroup("B", _clock);
        b.AddMatchday(1, _clock);
        a.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(b.Id))
            ]),
            _clock);
        b.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(a.Id))
            ]),
            _clock);

        var topology = CompetitionFlowAnalyzer.AnalyzeTopology([a, b], a.Id);

        topology.Should().Contain(d => d.Code == CompetitionFlowAnalyzer.CycleDetected);
        var act = () => PrepareStage.Execute(a, [a, b], _clock);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureCycleDetected);
    }

    [Fact]
    public void AnalyzeTopology_emits_NonRootUnfed_as_signal_only()
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

        var topology = CompetitionFlowAnalyzer.AnalyzeTopology([root, leaf], root.Id);

        topology.Should().ContainSingle(d =>
            d.Code == CompetitionFlowAnalyzer.NonRootUnfed && d.StageId == leaf.Id.Value);
        CompetitionFlowAnalyzer.IsPrepareGateIssue(CompetitionFlowAnalyzer.NonRootUnfed).Should().BeFalse();
        CompetitionFlowAnalyzer.IsStructureGraphGateIssue(CompetitionFlowAnalyzer.NonRootUnfed).Should().BeFalse();
    }

    [Fact]
    public void AnalyzeTopology_emits_RoutingNondeterminism_for_duplicate_qual_sources()
    {
        var competition = CreateCompetition.Execute("Routing", _clock);
        var source = Stage.Create(
            competition.Id,
            new StageName("Groups"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        var destA = Stage.Create(
            competition.Id,
            new StageName("DestA"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        var destB = Stage.Create(
            competition.Id,
            new StageName("DestB"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        competition.AddStage(source.Id, _clock);
        competition.AddStage(destA.Id, _clock);
        competition.AddStage(destB.Id, _clock);
        source.AddGroup("A", _clock);
        source.AddMatchday(1, _clock);
        source.SetPlacesPerGroup(4);
        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(destA.Id)),
                new QualificationPath(
                    2,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(destB.Id))
            ]),
            _clock);

        var topology = CompetitionFlowAnalyzer.AnalyzeTopology([source, destA, destB], source.Id);

        topology.Should().Contain(d => d.Code == CompetitionFlowAnalyzer.RoutingNondeterminism);
    }

    [Fact]
    public void AnalyzeCapacity_distinguishes_over_capacity_and_shortfall()
    {
        var competition = CreateCompetition.Execute("Capacity", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        competition.AddEntry(TeamId.New(), "C", _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("KO"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        competition.AddStage(stage.Id, _clock);
        stage.AddRound("QF", _clock);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        var a = competition.Entries[0].Id;
        var b = competition.Entries[1].Id;
        var c = competition.Entries[2].Id;
        stage.ReplaceAffectationAuthoring([a, b, c], _clock);

        var over = CompetitionFlowAnalyzer.AnalyzeCapacity([stage]);
        over.Should().Contain(d => d.Code == CompetitionFlowAnalyzer.OverCapacityEdge);
        over.Should().NotContain(d => d.Code == CompetitionFlowAnalyzer.PopulationShortfall);

        stage.ReplaceAffectationAuthoring([a], _clock);
        var shortfall = CompetitionFlowAnalyzer.AnalyzeCapacity([stage]);
        shortfall.Should().Contain(d => d.Code == CompetitionFlowAnalyzer.PopulationShortfall);
        CompetitionFlowAnalyzer.IsPrepareGateIssue(CompetitionFlowAnalyzer.PopulationShortfall).Should().BeFalse();
        CompetitionFlowAnalyzer.IsPrepareGateIssue(CompetitionFlowAnalyzer.OverCapacityEdge).Should().BeTrue();
    }

    [Fact]
    public void AnalyzeCapacity_detects_over_extraction()
    {
        var competition = CreateCompetition.Execute("Extract", _clock);
        var source = Stage.Create(
            competition.Id,
            new StageName("Groups"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        var dest = Stage.Create(
            competition.Id,
            new StageName("KO"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        competition.AddStage(source.Id, _clock);
        competition.AddStage(dest.Id, _clock);
        source.AddGroup("A", _clock);
        source.AddMatchday(1, _clock);
        source.SetPlacesPerGroup(2);
        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Top, 4),
                    QualificationDestination.ForPopulation(dest.Id))
            ]),
            _clock);

        var capacity = CompetitionFlowAnalyzer.AnalyzeCapacity([source, dest]);

        capacity.Should().Contain(d =>
            d.Code == CompetitionFlowAnalyzer.OverExtraction && d.StageId == source.Id.Value);
    }

    [Fact]
    public void AnalyzeResult_emits_authored_award_unreachable_when_source_pair_missing()
    {
        var competition = CreateCompetition.Execute("Result", _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("Final"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        competition.AddStage(stage.Id, _clock);
        stage.AddRound("F", _clock);
        stage.AddSlot("F-A");
        stage.AddSlot("F-B");
        stage.ReplaceBracketPairs([new BracketPair("FINAL", "F-A", "F-B")]);
        stage.ReplacePlacementAwardRules(
            new PlacementAwardRules(
            [
                new PlacementAwardPath("FINAL", ProgressionOutcome.Winner, rank: 1)
            ]),
            _clock);

        // Form can later drop the confrontation while award paths remain authored.
        stage.ClearBracketPairs();

        var result = CompetitionFlowAnalyzer.AnalyzeResult([stage]);

        result.Should().Contain(d => d.Code == CompetitionFlowAnalyzer.AuthoredAwardUnreachable);
        result.Should().Contain(d => d.Code == CompetitionFlowAnalyzer.ExpectedRankUnreachable);
        CompetitionFlowAnalyzer.AnalyzeExecution([stage]).Should().BeEmpty();
        CompetitionFlowAnalyzer.IsPrepareGateIssue(CompetitionFlowAnalyzer.AuthoredAwardUnreachable)
            .Should().BeFalse();
        CompetitionFlowAnalyzer.IsStructureGraphGateIssue(CompetitionFlowAnalyzer.AuthoredAwardUnreachable)
            .Should().BeFalse();
    }

    [Fact]
    public void AnalyzeResult_does_not_flag_competition_ranks_above_local_places_n()
    {
        var competition = CreateCompetition.Execute("Bronze", _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("Petite finale"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        competition.AddStage(stage.Id, _clock);
        stage.AddRound("B", _clock);
        stage.AddSlot("B-A");
        stage.AddSlot("B-B");
        stage.ReplaceBracketPairs([new BracketPair("BRONZE", "B-A", "B-B")]);
        stage.ReplacePlacementAwardRules(
            new PlacementAwardRules(
            [
                new PlacementAwardPath("BRONZE", ProgressionOutcome.Winner, rank: 3),
                new PlacementAwardPath("BRONZE", ProgressionOutcome.Loser, rank: 4)
            ]),
            _clock);

        var result = CompetitionFlowAnalyzer.AnalyzeResult([stage]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void AnalyzeResult_treats_bracket_pair_as_sufficient_without_fixture()
    {
        var competition = CreateCompetition.Execute("Pair only", _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("Finale"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        competition.AddStage(stage.Id, _clock);
        stage.AddRound("F", _clock);
        stage.AddSlot("F-A");
        stage.AddSlot("F-B");
        stage.ReplaceBracketPairs([new BracketPair("FINAL", "F-A", "F-B")]);
        stage.ReplacePlacementAwardRules(
            new PlacementAwardRules(
            [
                new PlacementAwardPath("FINAL", ProgressionOutcome.Winner, rank: 1),
                new PlacementAwardPath("FINAL", ProgressionOutcome.Loser, rank: 2)
            ]),
            _clock);

        stage.Rounds[0].Fixtures.Should().BeEmpty();
        CompetitionFlowAnalyzer.AnalyzeResult([stage]).Should().BeEmpty();
    }

    [Fact]
    public void Assemble_projects_capacity_gates_without_StructureGraphInvalid()
    {
        var competition = CreateCompetition.Execute("Cap ready", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        competition.AddEntry(TeamId.New(), "C", _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("KO"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        competition.AddStage(stage.Id, _clock);
        stage.AddRound("QF", _clock);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        stage.ReplaceAffectationAuthoring(
            [competition.Entries[0].Id, competition.Entries[1].Id, competition.Entries[2].Id],
            _clock);
        stage.AssignEntryToSlot("S1", competition.Entries[0].Id);
        stage.AssignEntryToSlot("S2", competition.Entries[1].Id);

        var view = StructureViewAssembler.Assemble(competition, [stage]);

        view.Stages.Single().StructureIssues.Should().Contain(StructureViewAssembler.IssueOverCapacityEdge);
        view.Readiness.Blockers.Should().Contain(StructureViewAssembler.BlockerOverCapacityEdge);
        view.Readiness.Blockers.Should().NotContain(StructureViewAssembler.BlockerStructureGraphInvalid);
    }
}
