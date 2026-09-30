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
    public void Assemble_projects_SlotFeedsInvalid_for_MultipleFeeds_not_for_Missing()
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

        // Missing feeds only — not projected as SlotFeedsInvalid in V1.
        var missingOnly = StructureViewAssembler.Assemble(competition, [source, target]);
        missingOnly.Stages.Single(hub => hub.StageId == target.Id.Value)
            .StructureIssues.Should().NotContain(StructureViewAssembler.IssueSlotFeedsInvalid);

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
            .StructureIssues.Should().Contain(StructureViewAssembler.IssueSlotFeedsInvalid);
        conflict.Readiness.Blockers.Should().Contain(StructureViewAssembler.BlockerStructureGraphInvalid);
    }
}
