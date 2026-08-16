// -----------------------------------------------------------------------
// <copyright file="CreateCompetitionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Competitions;

public sealed class CreateCompetitionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 8, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_creates_draft_competition_with_bootstrap_regulation()
    {
        var competition = CreateCompetition.Execute("U18 Cup", _clock);

        competition.Name.Value.Should().Be("U18 Cup");
        competition.Status.Should().Be(CompetitionStatus.Draft);
        competition.Entries.Should().BeEmpty();
        competition.StageIds.Should().BeEmpty();
        competition.Regulation.EntryRules.MinimumTeams.Should().Be(2);
    }

    [Fact]
    public void Execute_rejects_empty_name()
    {
        var act = () => CreateCompetition.Execute("   ", _clock);

        act.Should().Throw<DomainException>();
    }
}
