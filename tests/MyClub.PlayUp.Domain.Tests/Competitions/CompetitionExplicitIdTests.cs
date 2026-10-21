// -----------------------------------------------------------------------
// <copyright file="CompetitionExplicitIdTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Competitions.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Competitions;

public sealed class CompetitionExplicitIdTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Create_with_explicit_id_uses_that_identity()
    {
        var id = new CompetitionId(Guid.Parse("11111111-1111-5111-8111-111111111111"));

        var competition = Competition.Create(
            new CompetitionName("League"),
            SampleRegulations.Standard(),
            id,
            _clock);

        competition.Id.Should().Be(id);
        competition.Status.Should().Be(CompetitionStatus.Draft);
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionCreated>();
    }

    [Fact]
    public void Create_with_empty_id_throws()
    {
        var act = () => Competition.Create(
            new CompetitionName("League"),
            SampleRegulations.Standard(),
            new CompetitionId(Guid.Empty),
            _clock);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddEntry_with_explicit_entry_id_uses_that_identity()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var teamId = new TeamId(Guid.Parse("22222222-2222-5222-8222-222222222222"));
        var entryId = new EntryId(Guid.Parse("33333333-3333-5333-8333-333333333333"));

        var entry = competition.AddEntry(teamId, "Alpha", entryId, _clock);

        entry.Id.Should().Be(entryId);
        entry.TeamId.Should().Be(teamId);
        entry.DisplayName.Should().Be("Alpha");
    }

    [Fact]
    public void AddEntry_with_empty_entry_id_throws()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);

        var act = () => competition.AddEntry(TeamId.New(), "Alpha", new EntryId(Guid.Empty), _clock);

        act.Should().Throw<DomainException>();
    }
}
