// -----------------------------------------------------------------------
// <copyright file="PrepareCompetitionTests.cs" company="Stéphane ANDRE">
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

public sealed class PrepareCompetitionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 21, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_from_draft_with_stage_and_active_entry_prepares()
    {
        var competition = CreateDraftReadyToPrepare();

        PrepareCompetition.Execute(competition, _clock);

        competition.Status.Should().Be(CompetitionStatus.Ready);
    }

    [Fact]
    public void Execute_without_stage_rejects()
    {
        var competition = Competition.Create(new CompetitionName("No Stage"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);

        var act = () => PrepareCompetition.Execute(competition, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
        competition.Status.Should().Be(CompetitionStatus.Draft);
    }

    [Fact]
    public void Execute_without_active_entry_rejects()
    {
        var competition = Competition.Create(new CompetitionName("No Entry"), SampleRegulations.Standard(), _clock);
        competition.AddStage(StageId.New(), _clock);

        var act = () => PrepareCompetition.Execute(competition, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
        competition.Status.Should().Be(CompetitionStatus.Draft);
    }

    [Fact]
    public void Execute_from_ready_rejects()
    {
        var competition = CreateDraftReadyToPrepare();
        PrepareCompetition.Execute(competition, _clock);

        var act = () => PrepareCompetition.Execute(competition, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    private Competition CreateDraftReadyToPrepare()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddStage(StageId.New(), _clock);
        return competition;
    }
}
