// -----------------------------------------------------------------------
// <copyright file="MatchParticipantUnresolveTests.cs" company="Stéphane ANDRE">
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

public sealed class MatchParticipantUnresolveTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void UnresolveParticipant_clears_a_scheduled_side_and_emits_an_event()
    {
        var home = EntryId.New();
        var match = ScheduledMatch(home, EntryId.New());
        match.ClearDomainEvents();

        match.UnresolveParticipant(Side.Home, _clock);

        match.HomeEntryId.Should().BeNull();
        match.AwayEntryId.Should().NotBeNull();
        var unresolved = match.DomainEvents.Should().ContainSingle().Which
            .Should().BeOfType<MatchParticipantUnresolved>().Subject;
        unresolved.Side.Should().Be(Side.Home);
        unresolved.EntryId.Should().Be(home);
    }

    [Fact]
    public void UnresolveParticipant_accepts_a_postponed_match()
    {
        var home = EntryId.New();
        var match = ScheduledMatch(home, EntryId.New());
        match.Postpone(_clock);
        match.ClearDomainEvents();

        match.UnresolveParticipant(Side.Away, _clock);

        match.Status.Should().Be(MatchStatus.Postponed);
        match.AwayEntryId.Should().BeNull();
        match.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<MatchParticipantUnresolved>();
    }

    [Fact]
    public void UnresolveParticipant_is_a_no_op_when_the_side_is_already_unknown()
    {
        var match = ScheduledMatch(EntryId.New(), EntryId.New());
        match.UnresolveParticipant(Side.Home, _clock);
        match.ClearDomainEvents();

        match.UnresolveParticipant(Side.Home, _clock);

        match.HomeEntryId.Should().BeNull();
        match.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UnresolveParticipant_refuses_a_live_match()
    {
        var match = ScheduledMatch(EntryId.New(), EntryId.New());
        match.Start(_clock);

        var act = () => match.UnresolveParticipant(Side.Home, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.ParticipantImmutable);
        match.HomeEntryId.Should().NotBeNull();
    }

    [Fact]
    public void UnresolveParticipant_refuses_a_finished_match()
    {
        var match = ScheduledMatch(EntryId.New(), EntryId.New());
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);

        var act = () => match.UnresolveParticipant(Side.Home, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.ParticipantImmutable);
        match.HomeEntryId.Should().NotBeNull();
    }

    private Match ScheduledMatch(EntryId home, EntryId away) =>
        Match.Create(CompetitionId.New(), StageId.New(), home, away, _clock);
}
