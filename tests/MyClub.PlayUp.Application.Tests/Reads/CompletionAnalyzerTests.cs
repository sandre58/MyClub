// -----------------------------------------------------------------------
// <copyright file="CompletionAnalyzerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class CompletionAnalyzerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 11, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Analyze_when_all_matches_finished_and_no_pending_consequences_is_complete()
    {
        var ctx = CreateRunningWithFinishedMatch();

        var analysis = CompletionAnalyzer.Analyze(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = [ctx.Match] });

        analysis.IsSportivelyComplete.Should().BeTrue();
        analysis.CanCompleteNormally.Should().BeTrue();
        analysis.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_when_match_scheduled_is_incomplete()
    {
        var ctx = CreateRunningWithScheduledMatch();

        var analysis = CompletionAnalyzer.Analyze(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = [ctx.Match] });

        analysis.IsSportivelyComplete.Should().BeFalse();
        analysis.CanCompleteNormally.Should().BeFalse();
        analysis.Reasons.Should().ContainSingle(reason => reason.Code == CompletionAnalyzer.ReasonScheduledMatches);
    }

    [Fact]
    public void Analyze_when_match_live_is_incomplete()
    {
        var ctx = CreateRunningWithScheduledMatch();
        ctx.Match.Start(_clock);

        var analysis = CompletionAnalyzer.Analyze(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = [ctx.Match] });

        analysis.Reasons.Should().ContainSingle(reason => reason.Code == CompletionAnalyzer.ReasonLiveMatches);
    }

    [Fact]
    public void Analyze_when_match_postponed_is_incomplete()
    {
        var ctx = CreateRunningWithScheduledMatch();
        ctx.Match.Postpone(_clock);

        var analysis = CompletionAnalyzer.Analyze(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = [ctx.Match] });

        analysis.Reasons.Should().ContainSingle(reason => reason.Code == CompletionAnalyzer.ReasonPostponedMatches);
    }

    [Fact]
    public void Analyze_when_match_cancelled_does_not_block()
    {
        var ctx = CreateRunningWithScheduledMatch();
        ctx.Match.Cancel(_clock);

        var analysis = CompletionAnalyzer.Analyze(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = [ctx.Match] });

        analysis.IsSportivelyComplete.Should().BeTrue();
        analysis.CanCompleteNormally.Should().BeTrue();
    }

    [Fact]
    public void Analyze_when_progression_pending_is_incomplete()
    {
        var ctx = CreateFinishedKnockoutWithProgressionPending();

        var analysis = CompletionAnalyzer.Analyze(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = [ctx.Match] });

        analysis.IsSportivelyComplete.Should().BeFalse();
        analysis.Reasons.Should().Contain(reason =>
            reason.Code == NeedsAttentionAssembler.SourceProgressionPending);
    }

    [Fact]
    public void Analyze_when_draw_no_solution_is_incomplete()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.MarkDrawNoSolution(draw.Id, _clock);

        var analysis = CompletionAnalyzer.Analyze(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        analysis.Reasons.Should().Contain(reason =>
            reason.Code == NeedsAttentionAssembler.SourceDrawNoSolution);
    }

    [Fact]
    public void Analyze_does_not_mutate_competition()
    {
        var ctx = CreateRunningWithScheduledMatch();
        var before = ctx.Competition.Status;

        _ = CompletionAnalyzer.Analyze(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = [ctx.Match] });

        ctx.Competition.Status.Should().Be(before);
        ctx.Match.Status.Should().Be(MatchStatus.Scheduled);
    }

    [Fact]
    public void Analyze_when_no_matches_is_sportively_complete()
    {
        var competition = Competition.Create(new CompetitionName("Empty"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        var stage = Stage.Create(competition.Id, new StageName("S"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var analysis = CompletionAnalyzer.Analyze(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        analysis.IsSportivelyComplete.Should().BeTrue();
        analysis.CanCompleteNormally.Should().BeTrue();
    }

    private (Competition Competition, Stage Stage, Match Match) CreateRunningWithFinishedMatch()
    {
        var ctx = CreateRunningWithScheduledMatch();
        ctx.Match.Start(_clock);
        ctx.Match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        return ctx;
    }

    private (Competition Competition, Stage Stage, Match Match) CreateRunningWithScheduledMatch()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "A", _clock);
        var away = competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        return (competition, stage, match);
    }

    private (Competition Competition, Stage Stage, Match Match) CreateFinishedKnockoutWithProgressionPending()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("SF1-A");
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);

        return (competition, stage, match);
    }
}
