// -----------------------------------------------------------------------
// <copyright file="MatchParticipantReplacementTests.cs" company="Stéphane ANDRE">
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

public sealed class MatchParticipantReplacementTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void ReplaceParticipant_overwrites_a_known_side_and_emits_a_dedicated_event()
    {
        var match = Match.Create(
            CompetitionId.New(),
            StageId.New(),
            EntryId.New(),
            EntryId.New(),
            _clock);
        var replacement = EntryId.New();
        match.ClearDomainEvents();

        match.ReplaceParticipant(Side.Home, replacement, _clock);

        match.HomeEntryId.Should().Be(replacement);
        var replaced = match.DomainEvents.Should().ContainSingle().Which
            .Should().BeOfType<MatchParticipantReplaced>().Subject;
        replaced.Side.Should().Be(Side.Home);
        replaced.EntryId.Should().Be(replacement);
    }

    [Fact]
    public void ReplaceParticipant_refuses_a_live_match()
    {
        var home = EntryId.New();
        var match = Match.Create(CompetitionId.New(), StageId.New(), home, EntryId.New(), _clock);
        match.Start(_clock);

        var act = () => match.ReplaceParticipant(Side.Home, EntryId.New(), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.ParticipantImmutable);
        match.HomeEntryId.Should().Be(home);
        match.Status.Should().Be(MatchStatus.Live);
    }

    [Fact]
    public void ReplaceParticipant_refuses_a_finished_match_until_the_result_is_invalidated()
    {
        var home = EntryId.New();
        var match = Match.Create(CompetitionId.New(), StageId.New(), home, EntryId.New(), _clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        var replacement = EntryId.New();

        var blocked = () => match.ReplaceParticipant(Side.Home, replacement, _clock);

        blocked.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.ParticipantImmutable);
        match.HomeEntryId.Should().Be(home);
        match.Status.Should().Be(MatchStatus.Finished);

        match.InvalidateFinishedResult(_clock);
        match.ReplaceParticipant(Side.Home, replacement, _clock);

        match.Status.Should().Be(MatchStatus.Scheduled);
        match.HomeEntryId.Should().Be(replacement);
        match.Result.Should().BeNull();
    }

    [Fact]
    public void ReplaceParticipant_refuses_an_unknown_side()
    {
        var match = Match.Create(CompetitionId.New(), StageId.New(), homeEntryId: null, awayEntryId: null, _clock);

        var act = () => match.ReplaceParticipant(Side.Home, EntryId.New(), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.ParticipantNotResolved);
    }

    [Fact]
    public void ResolveParticipant_still_refuses_to_overwrite_a_known_side()
    {
        var home = EntryId.New();
        var match = Match.Create(CompetitionId.New(), StageId.New(), home, EntryId.New(), _clock);

        var act = () => match.ResolveParticipant(Side.Home, EntryId.New(), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.ParticipantAlreadyResolved);
        match.HomeEntryId.Should().Be(home);
    }

    [Fact]
    public void InvalidateFinishedResult_returns_the_match_to_not_played_and_drops_the_sheet()
    {
        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);
        var scorer = MemberId.New();
        match.AddDeclaredParticipation(scorer, Side.Home, CompositionStatus.Starter, _clock, jerseyNumber: 9);
        match.RecordGoal(scorer, Side.Home, _clock);
        match.ClearDomainEvents();

        match.InvalidateFinishedResult(_clock);

        match.Status.Should().Be(MatchStatus.Scheduled);
        match.Result.Should().BeNull();
        match.RunningScore.Should().BeNull();
        match.DeclaredParticipations.Should().BeEmpty();
        match.RecordedGoals.Should().BeEmpty();
        match.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<MatchFinishedResultInvalidated>();
    }

    [Fact]
    public void InvalidateFinishedResult_refuses_a_match_that_is_not_finished()
    {
        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);

        var act = () => match.InvalidateFinishedResult(_clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.FinishedResultMissing);
    }
}
