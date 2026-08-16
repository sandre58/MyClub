// -----------------------------------------------------------------------
// <copyright file="ArchiveCompetitionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Competitions;

public sealed class ArchiveCompetitionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 11, 45, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_from_completed_archives()
    {
        var competition = CreateCompleted();

        ArchiveCompetition.Execute(competition, _clock);

        competition.Status.Should().Be(CompetitionStatus.Archived);
    }

    [Fact]
    public void Execute_from_running_rejects()
    {
        var competition = Competition.Create(new CompetitionName("R"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddStage(StageId.New(), _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var act = () => ArchiveCompetition.Execute(competition, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Execute_from_draft_rejects()
    {
        var competition = Competition.Create(new CompetitionName("D"), SampleRegulations.Standard(), _clock);

        var act = () => ArchiveCompetition.Execute(competition, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Execute_twice_rejects()
    {
        var competition = CreateCompleted();
        ArchiveCompetition.Execute(competition, _clock);

        var act = () => ArchiveCompetition.Execute(competition, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    private Competition CreateCompleted()
    {
        var competition = Competition.Create(new CompetitionName("C"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddStage(StageId.New(), _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        competition.Complete(CompletionMode.Administrative, _clock);
        return competition;
    }
}
