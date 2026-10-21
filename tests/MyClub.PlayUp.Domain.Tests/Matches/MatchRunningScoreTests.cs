// -----------------------------------------------------------------------
// <copyright file="MatchRunningScoreTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Matches.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Matches;

public sealed class MatchRunningScoreTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 30, 11, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();
    private readonly StageId _stageId = StageId.New();
    private readonly EntryId _home = EntryId.New();
    private readonly EntryId _away = EntryId.New();

    [Fact]
    public void Case1_start_then_set_running_score()
    {
        var match = CreateMatch();
        match.Start(_clock);

        match.SetRunningScore(new RunningScore(1, 0), _clock);

        match.Status.Should().Be(MatchStatus.Live);
        match.RunningScore.Should().Be(new RunningScore(1, 0));
        match.Result.Should().BeNull();
        match.DomainEvents.OfType<MatchRunningScoreChanged>().Should().ContainSingle()
            .Which.RunningScore.Should().Be(new RunningScore(1, 0));
    }

    [Fact]
    public void Case2_absolute_correction_replaces_running_score()
    {
        var match = CreateLiveMatch();
        match.SetRunningScore(new RunningScore(2, 1), _clock);
        match.ClearDomainEvents();

        match.SetRunningScore(new RunningScore(1, 1), _clock);

        match.RunningScore.Should().Be(new RunningScore(1, 1));
        match.DomainEvents.OfType<MatchRunningScoreChanged>().Should().ContainSingle();
    }

    [Fact]
    public void Case3_finish_aligned_keeps_running_score_frozen_and_official_result()
    {
        var match = CreateLiveMatch();
        match.SetRunningScore(new RunningScore(2, 1), _clock);
        var result = new MatchResult(ResultType.Played, new Score(2, 1));

        match.Finish(result, _clock);

        match.Status.Should().Be(MatchStatus.Finished);
        match.Result.Should().Be(result);
        match.RunningScore.Should().Be(new RunningScore(2, 1));
        var mutate = () => match.SetRunningScore(new RunningScore(3, 1), _clock);
        mutate.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.RunningScoreImmutable);
    }

    [Fact]
    public void Case4_finish_divergent_does_not_require_running_score_equality()
    {
        var match = CreateLiveMatch();
        match.SetRunningScore(new RunningScore(2, 0), _clock);
        var result = new MatchResult(ResultType.Forfeit, new Score(0, 3));

        match.Finish(result, _clock);

        match.Result!.Score.Should().Be(new Score(0, 3));
        match.RunningScore.Should().Be(new RunningScore(2, 0));
    }

    [Fact]
    public void Case5_cancel_from_live_keeps_running_score_without_result()
    {
        var match = CreateLiveMatch();
        match.SetRunningScore(new RunningScore(1, 0), _clock);

        match.Cancel(_clock);

        match.Status.Should().Be(MatchStatus.Cancelled);
        match.Result.Should().BeNull();
        match.RunningScore.Should().Be(new RunningScore(1, 0));
        var mutate = () => match.SetRunningScore(new RunningScore(2, 0), _clock);
        mutate.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.RunningScoreImmutable);
    }

    [Fact]
    public void Case6_composition_remains_immutable_while_live()
    {
        var match = CreateLiveMatch();

        var act = () => match.AddDeclaredParticipation(
            MemberId.New(), Side.Home, CompositionStatus.Starter, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.CompositionImmutable);
    }

    [Fact]
    public void Case7_set_running_score_rejected_outside_live()
    {
        var scheduled = CreateMatch();
        var actScheduled = () => scheduled.SetRunningScore(new RunningScore(1, 0), _clock);

        actScheduled.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.RunningScoreImmutable);

        var finished = CreateLiveMatch();
        finished.Finish(new MatchResult(ResultType.Played, new Score(0, 0)), _clock);
        var actFinished = () => finished.SetRunningScore(new RunningScore(1, 0), _clock);
        actFinished.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.RunningScoreImmutable);
    }

    [Fact]
    public void Case8_start_initializes_running_score_to_zero_zero()
    {
        var match = CreateMatch();
        match.RunningScore.Should().BeNull();

        match.Start(_clock);

        match.RunningScore.Should().Be(new RunningScore(0, 0));
        match.DomainEvents.OfType<MatchRunningScoreChanged>().Should().BeEmpty();
    }

    [Fact]
    public void Cancel_before_start_leaves_running_score_null()
    {
        var match = CreateMatch();
        match.Cancel(_clock);

        match.Status.Should().Be(MatchStatus.Cancelled);
        match.RunningScore.Should().BeNull();
    }

    [Fact]
    public void Set_same_running_score_does_not_raise_event()
    {
        var match = CreateLiveMatch();
        match.SetRunningScore(new RunningScore(2, 0), _clock);
        match.ClearDomainEvents();

        match.SetRunningScore(new RunningScore(2, 0), _clock);

        match.DomainEvents.Should().BeEmpty();
        match.RunningScore.Should().Be(new RunningScore(2, 0));
    }

    [Fact]
    public void RunningScore_rejects_negative_goals()
    {
        var act = () => new RunningScore(-1, 0);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidRunningScore);
    }

    private Match CreateMatch() =>
        Match.Create(_competitionId, _stageId, _home, _away, _clock);

    private Match CreateLiveMatch()
    {
        var match = CreateMatch();
        match.Start(_clock);
        match.ClearDomainEvents();
        return match;
    }
}
