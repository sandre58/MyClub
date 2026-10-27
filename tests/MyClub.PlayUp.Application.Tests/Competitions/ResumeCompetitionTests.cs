// -----------------------------------------------------------------------
// <copyright file="ResumeCompetitionTests.cs" company="Stéphane ANDRE">
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

public sealed class ResumeCompetitionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 21, 10, 15, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_from_suspended_resumes()
    {
        var competition = CreateSuspended();

        ResumeCompetition.Execute(competition, _clock);

        competition.Status.Should().Be(CompetitionStatus.Running);
    }

    [Fact]
    public void Execute_from_running_rejects()
    {
        var competition = CreateRunning();

        var act = () => ResumeCompetition.Execute(competition, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
        competition.Status.Should().Be(CompetitionStatus.Running);
    }

    [Fact]
    public void Execute_from_ready_rejects()
    {
        var competition = CreateReady();

        var act = () => ResumeCompetition.Execute(competition, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
        competition.Status.Should().Be(CompetitionStatus.Ready);
    }

    private Competition CreateReady()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddStage(StageId.New(), _clock);
        competition.Prepare(_clock);
        return competition;
    }

    private Competition CreateRunning()
    {
        var competition = CreateReady();
        competition.Start(_clock);
        return competition;
    }

    private Competition CreateSuspended()
    {
        var competition = CreateRunning();
        competition.Suspend(_clock);
        return competition;
    }
}
