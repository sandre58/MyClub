// -----------------------------------------------------------------------
// <copyright file="CompleteCompetitionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Competitions;

public sealed class CompleteCompetitionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 11, 30, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_normal_when_complete_sets_completed()
    {
        var ctx = CreateRunningFinished();
        var analysis = CompletionAnalyzer.Analyze(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = [ctx.Match] });

        CompleteCompetition.Execute(ctx.Competition, CompletionMode.Normal, analysis, _clock);

        ctx.Competition.Status.Should().Be(CompetitionStatus.Completed);
        ctx.Competition.CompletionMode.Should().Be(CompletionMode.Normal);
    }

    [Fact]
    public void Execute_normal_when_incomplete_rejects_without_mutation()
    {
        var ctx = CreateRunningScheduled();
        var analysis = CompletionAnalyzer.Analyze(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = [ctx.Match] });

        var act = () => CompleteCompetition.Execute(ctx.Competition, CompletionMode.Normal, analysis, _clock);

        var ex = act.Should().Throw<ApplicationFailureException>().Which;
        ex.Code.Should().Be(ApplicationErrorCodes.CompletionNotAllowed);
        ex.Reasons.Should().Contain(CompletionAnalyzer.ReasonScheduledMatches);
        ctx.Competition.Status.Should().Be(CompetitionStatus.Running);
        ctx.Competition.CompletionMode.Should().BeNull();
    }

    [Theory]
    [InlineData(CompletionMode.Administrative)]
    [InlineData(CompletionMode.Abandoned)]
    public void Execute_exceptional_modes_allow_incomplete(CompletionMode mode)
    {
        var ctx = CreateRunningScheduled();
        var analysis = CompletionAnalyzer.Analyze(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = [ctx.Match] });

        CompleteCompetition.Execute(ctx.Competition, mode, analysis, _clock);

        ctx.Competition.Status.Should().Be(CompetitionStatus.Completed);
        ctx.Competition.CompletionMode.Should().Be(mode);
        ctx.Match.Status.Should().Be(MatchStatus.Scheduled);
    }

    [Fact]
    public void Execute_twice_rejects_via_domain()
    {
        var ctx = CreateRunningFinished();
        var analysis = CompletionAnalyzer.Analyze(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = [ctx.Match] });
        CompleteCompetition.Execute(ctx.Competition, CompletionMode.Normal, analysis, _clock);

        var act = () => CompleteCompetition.Execute(ctx.Competition, CompletionMode.Normal, analysis, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    private (Competition Competition, Stage Stage, Match Match) CreateRunningFinished()
    {
        var ctx = CreateRunningScheduled();
        ctx.Match.Start(_clock);
        ctx.Match.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        return ctx;
    }

    private (Competition Competition, Stage Stage, Match Match) CreateRunningScheduled()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "A", _clock);
        var away = competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        return (competition, stage, match);
    }
}
