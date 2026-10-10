// -----------------------------------------------------------------------
// <copyright file="CompleteStageTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class CompleteStageTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_when_all_matches_finished_completes_stage()
    {
        var (stage, match) = CreateRunningWithMatch();
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);

        CompleteStage.Execute(stage, [match], _clock);

        stage.Status.Should().Be(StageStatus.Completed);
    }

    [Fact]
    public void Execute_when_all_matches_cancelled_completes_stage()
    {
        var (stage, match) = CreateRunningWithMatch();
        match.Cancel(_clock);

        CompleteStage.Execute(stage, [match], _clock);

        stage.Status.Should().Be(StageStatus.Completed);
    }

    [Fact]
    public void Execute_when_no_matches_completes_running_stage()
    {
        var stage = CreateRunningWithoutMatches();

        CompleteStage.Execute(stage, [], _clock);

        stage.Status.Should().Be(StageStatus.Completed);
    }

    [Fact]
    public void Execute_when_scheduled_match_blocks_without_mutation()
    {
        var (stage, match) = CreateRunningWithMatch();

        var act = () => CompleteStage.Execute(stage, [match], _clock);

        var ex = act.Should().Throw<ApplicationFailureException>().Which;
        ex.Code.Should().Be(ApplicationErrorCodes.StageCompletionBlocked);
        ex.Reasons.Should().Contain(CompleteStage.ReasonOpenMatches);
        stage.Status.Should().Be(StageStatus.Running);
    }

    [Fact]
    public void Execute_when_not_running_rejects_via_domain()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        PrepareStage.Execute(stage, [stage], _clock);

        var act = () => CompleteStage.Execute(stage, [], _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
    }

    private (Stage Stage, Match Match) CreateRunningWithMatch()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "A", _clock);
        var away = competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        PrepareStage.Execute(stage, [stage], _clock);
        StartStage.Execute(stage, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        return (stage, match);
    }

    private Stage CreateRunningWithoutMatches()
    {
        var competition = Competition.Create(new CompetitionName("Empty"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        PrepareStage.Execute(stage, [stage], _clock);
        StartStage.Execute(stage, _clock);
        return stage;
    }
}
