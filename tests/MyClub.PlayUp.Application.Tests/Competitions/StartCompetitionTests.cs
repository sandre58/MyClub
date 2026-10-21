// -----------------------------------------------------------------------
// <copyright file="StartCompetitionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Competitions;

public sealed class StartCompetitionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 21, 10, 15, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_from_ready_starts()
    {
        var competition = CreateReady();

        StartCompetition.Execute(competition, _clock);

        competition.Status.Should().Be(CompetitionStatus.Running);
    }

    [Fact]
    public void Execute_from_draft_rejects()
    {
        var competition = Competition.Create(new CompetitionName("Draft"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddStage(StageId.New(), _clock);

        var act = () => StartCompetition.Execute(competition, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
        competition.Status.Should().Be(CompetitionStatus.Draft);
    }

    [Fact]
    public void Execute_from_running_rejects()
    {
        var competition = CreateReady();
        StartCompetition.Execute(competition, _clock);

        var act = () => StartCompetition.Execute(competition, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    private Competition CreateReady()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddStage(StageId.New(), _clock);
        competition.Prepare(_clock);
        return competition;
    }
}
