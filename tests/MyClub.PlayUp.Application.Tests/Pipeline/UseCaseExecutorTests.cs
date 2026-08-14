// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Moq;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

public sealed class UseCaseExecutorTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 17, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task PrepareStageAsync_loads_executes_and_saves_onceAsync()
    {
        var stage = CreateDraftChampionshipStage();
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = new UseCaseExecutor(stages.Object, unitOfWork.Object, _clock);
        await executor.PrepareStageAsync(stage.Id);

        stage.Status.Should().Be(StageStatus.Ready);
        stages.Verify(repository => repository.GetByIdAsync(stage.Id, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PrepareStageAsync_when_missing_throws_and_does_not_saveAsync()
    {
        var stageId = StageId.New();
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stage?)null);

        var executor = new UseCaseExecutor(stages.Object, unitOfWork.Object, _clock);
        var act = async () => await executor.PrepareStageAsync(stageId);

        var exception = await act.Should().ThrowAsync<ApplicationFailureException>();
        exception.Which.Code.Should().Be(ApplicationErrorCodes.StageNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PrepareStageAsync_when_domain_rejects_does_not_saveAsync()
    {
        var stage = CreateDraftChampionshipStage();
        PrepareStage.Execute(stage, [stage], _clock);
        stage.Status.Should().Be(StageStatus.Ready);

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);

        var executor = new UseCaseExecutor(stages.Object, unitOfWork.Object, _clock);
        var act = async () => await executor.PrepareStageAsync(stage.Id);

        var exception = await act.Should().ThrowAsync<DomainException>();
        exception.Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private Stage CreateDraftChampionshipStage()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("U15 League"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddMatchday(1, _clock);
        return stage;
    }
}
