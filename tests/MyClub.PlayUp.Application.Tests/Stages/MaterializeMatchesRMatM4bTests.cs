// -----------------------------------------------------------------------
// <copyright file="MaterializeMatchesRMatM4bTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// <see cref="CompetitionFlowAnalyzer.SourcePopulationShortfall"/> on a foreign stage
/// must not block <see cref="MaterializeMatches"/> on an independent championship.
/// </summary>
public sealed class MaterializeMatchesRMatM4BTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 15, 15, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_materializes_championship_S_when_foreign_groups_T_emits_SourcePopulationShortfall_without_batch_dependency()
    {
        var competition = CreateCompetition.Execute("R-Mat-M4b", _clock);
        var e1 = competition.AddEntry(TeamId.New(), "E1", _clock).Id;
        var e2 = competition.AddEntry(TeamId.New(), "E2", _clock).Id;
        var e3 = competition.AddEntry(TeamId.New(), "E3", _clock).Id;
        var e4 = competition.AddEntry(TeamId.New(), "E4", _clock).Id;

        var t = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        var ko = Stage.Create(
            competition.Id,
            new StageName("KO"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        competition.AddStage(t.Id, _clock);
        competition.AddStage(ko.Id, _clock);
        t.AddGroup("A", _clock);
        t.AddGroup("B", _clock);
        t.AddGroup("C", _clock);
        t.AddGroup("D", _clock);
        t.SetPlacesPerGroup(2);
        t.AddMatchday(1, _clock);
        t.ReplaceAffectationAuthoring([e1, e2], _clock);
        t.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Top, 4),
                    QualificationDestination.ForPopulation(ko.Id))
            ]),
            _clock);

        var s = Stage.Create(
            competition.Id,
            new StageName("Championnat"),
            SampleRegulations.Standard(),
            _clock);
        competition.AddStage(s.Id, _clock);
        s.AddMatchday(1, _clock);
        s.ReplaceAffectationAuthoring([e1, e2, e3, e4], _clock);

        var stages = new[] { t, ko, s };
        var capacity = CompetitionFlowAnalyzer.AnalyzeCapacity(stages, activeEntryCount: 4);
        capacity.Should().ContainSingle(d =>
            d.Code == CompetitionFlowAnalyzer.SourcePopulationShortfall && d.StageId == t.Id.Value);

        var qualTargetsS = stages.Any(other =>
            other.Regulation.QualificationRules?.Paths.Any(p => p.Destination.StageId.Equals(s.Id)) == true
            || other.Regulation.ProgressionRules?.Paths.Any(p => p.Destination.StageId.Equals(s.Id)) == true);
        qualTargetsS.Should().BeFalse("batch on S must not depend on extraction from T");

        StageMaterializationTestSupport.EnsureReadyForMaterialize(competition, s, stages, _clock);

        var result = MaterializeMatches.Execute(competition, s, [], _clock, stages);

        result.CreatedMatches.Should().HaveCount(6);
        t.Matchdays.Sum(md => md.Fixtures.Count).Should().Be(0);
    }

    [Fact]
    public void Execute_on_qual_fed_championship_with_incomplete_roster_uses_MaterializationFailure()
    {
        var competition = Competition.Create(new CompetitionName("M4aOverlap"), SampleRegulations.Standard(), _clock);
        var t = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        competition.AddStage(t.Id, _clock);
        t.AddGroup("A", _clock);
        t.AddGroup("B", _clock);
        t.AddMatchday(1, _clock);

        var s = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        s.AddMatchday(1, _clock);
        competition.AddStage(s.Id, _clock);
        var e1 = competition.AddEntry(TeamId.New(), "E1", _clock).Id;
        var e2 = competition.AddEntry(TeamId.New(), "E2", _clock).Id;
        s.ReplaceAffectationAuthoring([e1, e2], _clock);

        ReplaceStageQualificationRules.Execute(
            t,
            [
                new QualificationIntentSpec(
                    Guid.NewGuid(),
                    1,
                    QualificationIntentSourceKind.EachGroup,
                    1,
                    1,
                    s.Id.Value,
                    DestinationForm: true)
            ],
            _clock);

        PrepareStage.Execute(s, [t, s], _clock);

        var act = () => MaterializeMatches.Execute(competition, s, [], _clock, [t, s]);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.MaterializationFailure);
    }
}
