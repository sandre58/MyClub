// -----------------------------------------------------------------------
// <copyright file="ReplaceStageStandingRulesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class ReplaceStageStandingRulesTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 9, 16, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Replace_updates_points_and_criteria_while_running()
    {
        var (competition, stage) = CreateRunningChampionship();
        var replacement = new StandingRules(
            new PointsPolicy(2, 1, 0),
            [RankingCriterion.Points, RankingCriterion.Wins]);

        ReplaceStageStandingRules.Execute(stage, competition, replacement, _clock);

        stage.Status.Should().Be(StageStatus.Running);
        stage.Regulation.StandingRules.Should().Be(replacement);
    }

    [Fact]
    public void Replace_rejects_non_classifying_cup()
    {
        var competition = CreateCompetition.Execute("Cup-Standing", _clock);
        var stage = AddCompetitionStage.Execute(competition, StructureIntent.Cup(2, "KO"), _clock);
        stage.ClearStandingRules(_clock);
        stage.AddRound("Final", _clock);

        var act = () => ReplaceStageStandingRules.Execute(
            stage,
            competition,
            new StandingRules(new PointsPolicy(3, 1, 0), [RankingCriterion.Points]),
            _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StandingRulesInvariant);
    }

    [Fact]
    public void Replace_rejects_when_ready()
    {
        var (competition, stage) = CreateReadyChampionship();

        var act = () => ReplaceStageStandingRules.Execute(
            stage,
            competition,
            new StandingRules(new PointsPolicy(3, 1, 0), [RankingCriterion.Points]),
            _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StandingRulesLocked);
    }

    [Fact]
    public void Replace_rejects_when_completed()
    {
        var (competition, stage) = CreateRunningChampionship();
        stage.Complete(_clock);

        var act = () => ReplaceStageStandingRules.Execute(
            stage,
            competition,
            new StandingRules(new PointsPolicy(3, 1, 0), [RankingCriterion.Points]),
            _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
    }

    private (Competition Competition, Stage Stage) CreateReadyChampionship()
    {
        var competition = CreateCompetition.Execute("League-Standing-Ready", _clock);
        var stage = AddCompetitionStage.Execute(competition, StructureIntent.Championship(stageName: "Championship"), _clock);
        stage.Prepare(_clock);
        return (competition, stage);
    }

    private (Competition Competition, Stage Stage) CreateRunningChampionship()
    {
        var (competition, stage) = CreateReadyChampionship();
        stage.Start(_clock);
        return (competition, stage);
    }
}
