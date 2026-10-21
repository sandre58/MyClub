// -----------------------------------------------------------------------
// <copyright file="StructureViewAssemblerIssuesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructureViewAssemblerIssuesTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_projects_dangling_qualification_and_StructureGraphInvalid()
    {
        var competition = CreateCompetition.Execute("Graph invalid", _clock);
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

        var ghostStageId = StageId.New();
        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(ghostStageId))
            ]),
            _clock);

        var view = StructureViewAssembler.Assemble(competition, [source]);

        view.Stages.Should().ContainSingle()
            .Which.StructureIssues.Should().Contain(StructureViewAssembler.IssueDanglingQualificationTarget);
        view.Readiness.Blockers.Should().Contain(StructureViewAssembler.BlockerStructureGraphInvalid);
        view.Readiness.ReadyForNextSlice.Should().BeFalse();
    }

    [Fact]
    public void Assemble_projects_SlotFeedMissing_without_StructureGraphInvalid()
    {
        var competition = CreateCompetition.Execute("Feeds missing", _clock);
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
        var targetHub = view.Stages.Single(hub => hub.StageId == target.Id.Value);

        targetHub.StructureIssues.Should().Contain(StructureViewAssembler.IssueSlotFeedMissing);
        targetHub.StructureIssues.Should().NotContain(StructureViewAssembler.IssueSlotFeedInvalid);
        targetHub.StructureIssues.Should().NotContain(StructureViewAssembler.IssueSlotFeedMultiple);
        view.Readiness.Blockers.Should().NotContain(StructureViewAssembler.BlockerStructureGraphInvalid);
    }

    [Fact]
    public void Assemble_projects_SlotFeedMultiple_as_StructureGraphInvalid()
    {
        var competition = CreateCompetition.Execute("Feeds conflict", _clock);
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

        var directEntry = EntryId.New();
        target.ReplaceAffectationAuthoring([directEntry], _clock);
        target.AssignEntryToSlot("SF1-A", directEntry);
        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(target.Id, "SF1-A"))
            ]),
            _clock);

        var conflict = StructureViewAssembler.Assemble(competition, [source, target]);
        conflict.Stages.Single(hub => hub.StageId == target.Id.Value)
            .StructureIssues.Should().Contain(StructureViewAssembler.IssueSlotFeedMultiple);
        conflict.Readiness.Blockers.Should().Contain(StructureViewAssembler.BlockerStructureGraphInvalid);
    }

    [Fact]
    public void Assemble_projects_ConfrontationUndecidable_without_StructureGraphInvalid()
    {
        var competition = CreateCompetition.Execute("Exec undecidable", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);

        var stage = Stage.Create(
            competition.Id,
            new StageName("KO"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        competition.AddStage(stage.Id, _clock);
        var round = stage.AddRound("QF", tieFormat: null, _clock);
        stage.AddFixture(round.Id, _clock);
        stage.AddSlot("KO-A");
        stage.AddSlot("KO-B");
        stage.AddSlot("SF1-A");
        stage.ReplaceBracketPairs([new BracketPair("P1", "KO-A", "KO-B")]);
        var a = competition.Entries[0].Id;
        var b = competition.Entries[1].Id;
        stage.ReplaceAffectationAuthoring([a, b], _clock);
        stage.AssignEntryToSlot("KO-A", a);
        stage.AssignEntryToSlot("KO-B", b);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    "P1",
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);

        var view = StructureViewAssembler.Assemble(competition, [stage]);
        var hub = view.Stages.Should().ContainSingle().Subject;

        hub.StructureIssues.Should().Contain(StructureViewAssembler.IssueConfrontationUndecidable);
        view.Readiness.Blockers.Should().Contain(StructureViewAssembler.BlockerConfrontationUndecidable);
        view.Readiness.Blockers.Should().NotContain(StructureViewAssembler.BlockerStructureGraphInvalid);
    }

    [Fact]
    public void AnalyzeFeeds_emits_one_diagnostic_per_non_unique_slot()
    {
        var competition = CreateCompetition.Execute("Feed analyzer", _clock);
        var target = Stage.Create(
            competition.Id,
            new StageName("KO"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        competition.AddStage(target.Id, _clock);
        target.AddRound("QF", _clock);
        target.AddSlot("S1");
        target.AddSlot("S2");

        var feeds = CompetitionFlowAnalyzer.AnalyzeFeeds([target]);

        feeds.Should().HaveCount(2);
        feeds.Should().OnlyContain(d => d.Code == CompetitionFlowAnalyzer.SlotFeedMissing);
        feeds.Select(d => d.SlotKey).Should().BeEquivalentTo("S1", "S2");
    }
}
