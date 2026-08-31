// -----------------------------------------------------------------------
// <copyright file="SetRunningScoreUseCaseTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Matches;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Matches.Events;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Matches;

public sealed class SetRunningScoreUseCaseTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 31, 8, 35, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_updates_running_score_on_live_match()
    {
        var match = CreateLiveMatch();
        match.ClearDomainEvents();

        SetRunningScore.Execute(match, 2, 1, _clock);

        match.RunningScore.Should().Be(new RunningScore(2, 1));
        match.DomainEvents.OfType<MatchRunningScoreChanged>().Should().ContainSingle()
            .Which.RunningScore.Should().Be(new RunningScore(2, 1));
    }

    [Fact]
    public void Execute_rejects_when_match_is_not_live()
    {
        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);

        var act = () => SetRunningScore.Execute(match, 1, 0, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.RunningScoreImmutable);
    }

    [Fact]
    public void Execute_rejects_negative_goals()
    {
        var match = CreateLiveMatch();

        var act = () => SetRunningScore.Execute(match, -1, 0, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidRunningScore);
    }

    [Fact]
    public void Execute_noop_when_score_unchanged()
    {
        var match = CreateLiveMatch();
        match.SetRunningScore(new RunningScore(1, 0), _clock);
        match.ClearDomainEvents();

        SetRunningScore.Execute(match, 1, 0, _clock);

        match.RunningScore.Should().Be(new RunningScore(1, 0));
        match.DomainEvents.Should().BeEmpty();
    }

    private Match CreateLiveMatch()
    {
        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        match.Start(_clock);
        return match;
    }
}
