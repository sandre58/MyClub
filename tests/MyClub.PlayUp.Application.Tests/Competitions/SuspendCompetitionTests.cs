// -----------------------------------------------------------------------
// <copyright file="SuspendCompetitionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Competitions;

public sealed class SuspendCompetitionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 21, 10, 15, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_from_running_suspends()
    {
        var competition = CreateRunning();

        SuspendCompetition.Execute(competition, _clock);

        competition.Status.Should().Be(CompetitionStatus.Suspended);
    }

    [Fact]
    public void Execute_from_ready_rejects()
    {
        var competition = CreateReady();

        var act = () => SuspendCompetition.Execute(competition, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
        competition.Status.Should().Be(CompetitionStatus.Ready);
    }

    [Fact]
    public void Execute_from_suspended_rejects()
    {
        var competition = CreateRunning();
        SuspendCompetition.Execute(competition, _clock);

        var act = () => SuspendCompetition.Execute(competition, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
        competition.Status.Should().Be(CompetitionStatus.Suspended);
    }

    [Fact]
    public void Resume_from_suspended_returns_running_and_leaves_stage_suspended()
    {
        var situation = TestCompetition.Create("Groups suspended", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship())
            .PrepareCompetition()
            .PreparePrimaryStage()
            .StartCompetition()
            .StartPrimaryStage();
        var stage = situation.RequirePrimaryStage();
        stage.Suspend(_clock);
        SuspendCompetition.Execute(situation.Competition, _clock);

        ResumeCompetition.Execute(situation.Competition, _clock);

        situation.Competition.Status.Should().Be(CompetitionStatus.Running);
        stage.Status.Should().Be(StageStatus.Suspended);
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
}
