// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorReadTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Moq;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;
using DomainMatch = MyClub.PlayUp.Domain.Matches.Match;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

public sealed class UseCaseExecutorReadTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 22, 30, 0, TimeSpan.Zero));

    [Fact]
    public async Task GetCompetitionOverviewAsync_when_missing_throws_and_does_not_saveAsync()
    {
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        competitions
            .Setup(repository => repository.GetByIdAsync(It.IsAny<CompetitionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Competition?)null);

        var executor = new UseCaseExecutor(stages.Object, matches.Object, competitions.Object, unitOfWork.Object, _clock, AlwaysExistingMedia.Instance);
        var act = async () => await executor.GetCompetitionOverviewAsync(CompetitionId.New());

        (await act.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.CompetitionNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStageOverviewAsync_when_missing_throws_and_does_not_saveAsync()
    {
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        stages
            .Setup(repository => repository.GetByIdAsync(It.IsAny<StageId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stage?)null);

        var executor = new UseCaseExecutor(stages.Object, matches.Object, competitions.Object, unitOfWork.Object, _clock, AlwaysExistingMedia.Instance);
        var act = async () => await executor.GetStageOverviewAsync(StageId.New());

        (await act.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.StageNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetMatchDetailAsync_when_missing_throws_and_does_not_saveAsync()
    {
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        matches
            .Setup(repository => repository.GetByIdAsync(It.IsAny<MatchId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainMatch?)null);

        var executor = new UseCaseExecutor(stages.Object, matches.Object, competitions.Object, unitOfWork.Object, _clock, AlwaysExistingMedia.Instance);
        var act = async () => await executor.GetMatchDetailAsync(MatchId.New());

        (await act.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.MatchNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListMatchesByStageAsync_loads_and_assembles_without_saveAsync()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        competition.AddStage(stage.Id, _clock);
        var match = DomainMatch.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);

        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages.Setup(repository => repository.GetByIdAsync(stage.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stage);
        competitions
            .Setup(repository => repository.GetByIdAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        matches
            .Setup(repository => repository.ListByStageAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([match]);

        var executor = new UseCaseExecutor(stages.Object, matches.Object, competitions.Object, unitOfWork.Object, _clock, AlwaysExistingMedia.Instance);
        var summaries = await executor.ListMatchesByStageAsync(stage.Id);

        summaries.Should().ContainSingle();
        summaries[0].Home.DisplayName.Should().Be("Home");
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
