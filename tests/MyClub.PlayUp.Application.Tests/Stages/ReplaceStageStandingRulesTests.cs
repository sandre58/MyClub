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
        var stage = CreateRunningChampionship();
        var replacement = new StandingRules(
            new PointsPolicy(2, 1, 0),
            [RankingCriterion.Points, RankingCriterion.Wins]);

        ReplaceStageStandingRules.Execute(stage, replacement, _clock);

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
            new StandingRules(new PointsPolicy(3, 1, 0), [RankingCriterion.Points]),
            _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StandingRulesInvariant);
    }

    [Fact]
    public void Replace_rejects_when_completed()
    {
        var stage = CreateRunningChampionship();
        stage.Complete(_clock);

        var act = () => ReplaceStageStandingRules.Execute(
            stage,
            new StandingRules(new PointsPolicy(3, 1, 0), [RankingCriterion.Points]),
            _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
    }

    private Stage CreateRunningChampionship()
    {
        var competition = CreateCompetition.Execute("League-Standing", _clock);
        var stage = AddCompetitionStage.Execute(competition, StructureIntent.Championship(stageName: "Championship"), _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        return stage;
    }
}
