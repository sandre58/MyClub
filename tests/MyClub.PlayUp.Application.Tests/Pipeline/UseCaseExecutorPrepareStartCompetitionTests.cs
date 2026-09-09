// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorPrepareStartCompetitionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

public sealed class UseCaseExecutorPrepareStartCompetitionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 21, 11, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task PrepareCompetitionAsync_persists_onceAsync()
    {
        var competition = CreateDraftReadyToPrepare();
        var (executor, unitOfWork) = CreateExecutor(competition);

        await executor.PrepareCompetitionAsync(competition.Id);

        competition.Status.Should().Be(CompetitionStatus.Ready);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PrepareCompetitionAsync_when_domain_rejects_does_not_saveAsync()
    {
        var competition = Competition.Create(new CompetitionName("Thin"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        var (executor, unitOfWork) = CreateExecutor(competition);

        var act = () => executor.PrepareCompetitionAsync(competition.Id);

        await act.Should().ThrowAsync<DomainException>()
            .Where(ex => ex.Code == CompetitionErrorCodes.InvalidTransition);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        competition.Status.Should().Be(CompetitionStatus.Draft);
    }

    [Fact]
    public async Task PrepareCompetitionAsync_when_missing_throws_and_does_not_saveAsync()
    {
        var missingId = CompetitionId.New();
        var (executor, unitOfWork) = CreateExecutor(competition: null, missingId);

        var act = () => executor.PrepareCompetitionAsync(missingId);

        await act.Should().ThrowAsync<ApplicationFailureException>()
            .Where(ex => ex.Code == ApplicationErrorCodes.CompetitionNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PrepareCompetitionAsync_when_completed_returns_competition_closedAsync()
    {
        var competition = CreateCompleted();
        var (executor, unitOfWork) = CreateExecutor(competition);

        var act = () => executor.PrepareCompetitionAsync(competition.Id);

        await act.Should().ThrowAsync<ApplicationFailureException>()
            .Where(ex => ex.Code == ApplicationErrorCodes.CompetitionClosed);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartCompetitionAsync_persists_onceAsync()
    {
        var competition = CreateReady();
        var (executor, unitOfWork) = CreateExecutor(competition);

        await executor.StartCompetitionAsync(competition.Id);

        competition.Status.Should().Be(CompetitionStatus.Running);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartCompetitionAsync_when_domain_rejects_does_not_saveAsync()
    {
        var competition = CreateDraftReadyToPrepare();
        var (executor, unitOfWork) = CreateExecutor(competition);

        var act = () => executor.StartCompetitionAsync(competition.Id);

        await act.Should().ThrowAsync<DomainException>()
            .Where(ex => ex.Code == CompetitionErrorCodes.InvalidTransition);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartCompetitionAsync_when_missing_throws_and_does_not_saveAsync()
    {
        var missingId = CompetitionId.New();
        var (executor, unitOfWork) = CreateExecutor(competition: null, missingId);

        var act = () => executor.StartCompetitionAsync(missingId);

        await act.Should().ThrowAsync<ApplicationFailureException>()
            .Where(ex => ex.Code == ApplicationErrorCodes.CompetitionNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartCompetitionAsync_when_completed_returns_competition_closedAsync()
    {
        var competition = CreateCompleted();
        var (executor, unitOfWork) = CreateExecutor(competition);

        var act = () => executor.StartCompetitionAsync(competition.Id);

        await act.Should().ThrowAsync<ApplicationFailureException>()
            .Where(ex => ex.Code == ApplicationErrorCodes.CompetitionClosed);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private (UseCaseExecutor Executor, Mock<IUnitOfWork> UnitOfWork) CreateExecutor(
        Competition? competition,
        CompetitionId? missingId = null)
    {
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        if (competition is not null)
        {
            competitions
                .Setup(repository => repository.GetByIdForUpdateAsync(competition.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(competition);
        }
        else
        {
            competitions
                .Setup(repository => repository.GetByIdForUpdateAsync(missingId!.Value, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Competition?)null);
        }

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = new UseCaseExecutor(stages.Object, matches.Object, competitions.Object, unitOfWork.Object, _clock, AlwaysExistingMedia.Instance, NullLogger<UseCaseExecutor>.Instance);
        return (executor, unitOfWork);
    }

    private Competition CreateDraftReadyToPrepare()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddStage(StageId.New(), _clock);
        return competition;
    }

    private Competition CreateReady()
    {
        var competition = CreateDraftReadyToPrepare();
        competition.Prepare(_clock);
        return competition;
    }

    private Competition CreateCompleted()
    {
        var competition = CreateReady();
        competition.Start(_clock);
        competition.Complete(CompletionMode.Administrative, _clock);
        return competition;
    }
}
