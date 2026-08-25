// -----------------------------------------------------------------------
// <copyright file="MatchExplicitIdTests.cs" company="Stéphane ANDRE">
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

public sealed class MatchExplicitIdTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 6, 15, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();
    private readonly StageId _stageId = StageId.New();
    private readonly EntryId _home = EntryId.New();
    private readonly EntryId _away = EntryId.New();

    [Fact]
    public void Create_with_explicit_id_uses_that_identity()
    {
        var id = new MatchId(Guid.Parse("55555555-5555-5555-8555-555555555555"));

        var match = Match.Create(_competitionId, _stageId, _home, _away, id, _clock);

        match.Id.Should().Be(id);
        match.Status.Should().Be(MatchStatus.Scheduled);
        match.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<MatchCreated>();
    }

    [Fact]
    public void Create_with_empty_id_throws()
    {
        var act = () => Match.Create(
            _competitionId,
            _stageId,
            _home,
            _away,
            new MatchId(Guid.Empty),
            _clock);

        act.Should().Throw<DomainException>();
    }
}
