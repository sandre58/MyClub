// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorRest6LifecycleTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

/// <summary>
/// A10-REST-6 — competition lifecycle suspend/resume and missing-competition envelopes.
/// </summary>
public sealed class UseCaseExecutorRest6LifecycleTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 18, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task SuspendCompetitionAsync_persists_once_under_competition_lockAsync()
    {
        var competition = CreateRunning();
        var (executor, unitOfWork) = CreateExecutor(competition);

        await executor.SuspendCompetitionAsync(competition.Id);

        competition.Status.Should().Be(CompetitionStatus.Suspended);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResumeCompetitionAsync_persists_once_under_competition_lockAsync()
    {
        var competition = CreateRunning();
        competition.Suspend(_clock);
        var (executor, unitOfWork) = CreateExecutor(competition);

        await executor.ResumeCompetitionAsync(competition.Id);

        competition.Status.Should().Be(CompetitionStatus.Running);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SuspendCompetitionAsync_when_missing_throws_after_entering_exclusive_writeAsync()
    {
        var missingId = CompetitionId.New();
        var (executor, unitOfWork) = CreateExecutor(competition: null, missingId);

        var act = () => executor.SuspendCompetitionAsync(missingId);

        await act.Should().ThrowAsync<ApplicationFailureException>()
            .Where(ex => ex.Code == ApplicationErrorCodes.CompetitionNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                missingId,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResumeCompetitionAsync_when_missing_throws_after_entering_exclusive_writeAsync()
    {
        var missingId = CompetitionId.New();
        var (executor, unitOfWork) = CreateExecutor(competition: null, missingId);

        var act = () => executor.ResumeCompetitionAsync(missingId);

        await act.Should().ThrowAsync<ApplicationFailureException>()
            .Where(ex => ex.Code == ApplicationErrorCodes.CompetitionNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                missingId,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CompleteCompetitionAsync_when_missing_throws_after_entering_exclusive_writeAsync()
    {
        var missingId = CompetitionId.New();
        var (executor, unitOfWork) = CreateExecutor(competition: null, missingId);

        var act = () => executor.CompleteCompetitionAsync(missingId, CompletionMode.Administrative);

        await act.Should().ThrowAsync<ApplicationFailureException>()
            .Where(ex => ex.Code == ApplicationErrorCodes.CompetitionNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                missingId,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ArchiveCompetitionAsync_when_missing_throws_after_entering_exclusive_writeAsync()
    {
        var missingId = CompetitionId.New();
        var (executor, unitOfWork) = CreateExecutor(competition: null, missingId);

        var act = () => executor.ArchiveCompetitionAsync(missingId);

        await act.Should().ThrowAsync<ApplicationFailureException>()
            .Where(ex => ex.Code == ApplicationErrorCodes.CompetitionNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                missingId,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddEntryAsync_when_missing_throws_after_entering_exclusive_write_without_structure_viewAsync()
    {
        var missingId = CompetitionId.New();
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Competition?)null);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        unitOfWork
            .Setup(uow => uow.ExecuteExclusiveAsync(
                missingId,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns((CompetitionId _, Func<CancellationToken, Task> write, CancellationToken token) => write(token));

        var executor = new UseCaseExecutor(
            stages.Object,
            matches.Object,
            competitions.Object,
            unitOfWork.Object,
            _clock,
            AlwaysExistingMedia.Instance,
            NullLogger<UseCaseExecutor>.Instance);

        var act = () => executor.AddEntryAsync(missingId, "Late Team");

        await act.Should().ThrowAsync<ApplicationFailureException>()
            .Where(ex => ex.Code == ApplicationErrorCodes.CompetitionNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                missingId,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        matches.VerifyNoOtherCalls();
        stages.VerifyNoOtherCalls();
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
        unitOfWork
            .Setup(uow => uow.ExecuteExclusiveAsync(
                It.IsAny<CompetitionId>(),
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns((CompetitionId _, Func<CancellationToken, Task> write, CancellationToken token) => write(token));

        var executor = new UseCaseExecutor(
            stages.Object,
            matches.Object,
            competitions.Object,
            unitOfWork.Object,
            _clock,
            AlwaysExistingMedia.Instance,
            NullLogger<UseCaseExecutor>.Instance);
        return (executor, unitOfWork);
    }

    private Competition CreateRunning()
    {
        var competition = Competition.Create(new CompetitionName("REST-6"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddStage(StageId.New(), _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        return competition;
    }
}
