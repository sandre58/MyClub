// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorDrawLifecycleLogTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
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
/// DrawPublished / DrawCancelled logs are emitted only after exclusive write commit.
/// </summary>
public sealed class UseCaseExecutorDrawLifecycleLogTests
{
    private const int DrawPublishedEventId = 1020;
    private const int DrawCancelledEventId = 1022;

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 15, 0, 0, TimeSpan.Zero));
    private readonly CollectingLogger _logger = new();

    [Theory]
    [InlineData(DrawPublishedEventId, "Publish")]
    [InlineData(DrawCancelledEventId, "Cancel")]
    public async Task Draw_lifecycle_logs_after_exclusive_write_completesAsync(int eventId, string operation)
    {
        var (executor, stageId, drawId) = CreateExecutorForSuccessfulDraw(operation, simulateCommitFailure: false);

        await InvokeDrawAsync(executor, stageId, drawId, operation);

        _logger.Entries.Should().ContainSingle(e => e.EventId == eventId);
    }

    [Fact]
    public async Task PublishAndApplyDrawAsync_emits_log_1020_after_publish_step_commitAsync()
    {
        var (executor, stageId, drawId) = CreateExecutorForPublishAndApply(simulatePublishCommitFailure: false);

        await executor.PublishAndApplyDrawAsync(stageId, drawId);

        _logger.Entries.Where(entry => entry.EventId == DrawPublishedEventId)
            .Should()
            .ContainSingle();
    }

    [Fact]
    public async Task PublishAndApplyDrawAsync_publish_step_does_not_emit_log_1020_when_commit_failsAsync()
    {
        var (executor, stageId, drawId) = CreateExecutorForPublishAndApply(simulatePublishCommitFailure: true);

        var act = async () => await executor.PublishAndApplyDrawAsync(stageId, drawId);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _logger.Entries.Should().NotContain(entry => entry.EventId == DrawPublishedEventId);
    }

    [Theory]
    [InlineData("Publish")]
    [InlineData("Cancel")]
    public async Task Draw_lifecycle_does_not_log_when_exclusive_write_fails_after_saveAsync(string operation)
    {
        var (executor, stageId, drawId) = CreateExecutorForSuccessfulDraw(operation, simulateCommitFailure: true);

        var act = async () => await InvokeDrawAsync(executor, stageId, drawId, operation);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _logger.Entries.Should().BeEmpty();
    }

    private static Task InvokeDrawAsync(UseCaseExecutor executor, StageId stageId, DrawId drawId, string operation) =>
        operation switch
        {
            "Publish" => executor.PublishDrawAsync(stageId, drawId),
            "Cancel" => executor.CancelDrawAsync(stageId, drawId),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };

    private (UseCaseExecutor Executor, StageId StageId, DrawId DrawId) CreateExecutorForSuccessfulDraw(
        string operation,
        bool simulateCommitFailure)
    {
        var competition = Competition.Create(new CompetitionName("Draw Log"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("A");
        var entry = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry], _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry, "A")]),
            _clock);
        if (operation == "Cancel")
        {
            stage.PublishDraw(draw.Id, _clock);
        }

        competition.AddStage(stage.Id, _clock);

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetCompetitionIdReadOnlyAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition.Id);
        stages
            .Setup(repository => repository.GetByIdForUpdateAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (CompetitionId _, Func<CancellationToken, Task> write, CancellationToken token) =>
            {
                await write(token).ConfigureAwait(false);
                if (simulateCommitFailure)
                {
                    throw new InvalidOperationException("simulated transaction commit failure");
                }
            });

        var executor = new UseCaseExecutor(
            stages.Object,
            matchRepo.Object,
            competitions.Object,
            unitOfWork.Object,
            _clock,
            AlwaysExistingMedia.Instance,
            _logger);

        return (executor, stage.Id, draw.Id);
    }

    private (UseCaseExecutor Executor, StageId StageId, DrawId DrawId) CreateExecutorForPublishAndApply(
        bool simulatePublishCommitFailure)
    {
        var competition = Competition.Create(new CompetitionName("Draw Log P+A"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("A");
        var entry = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry], _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry, "A")]),
            _clock);
        competition.AddStage(stage.Id, _clock);

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetCompetitionIdReadOnlyAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition.Id);
        stages
            .Setup(repository => repository.GetByIdForUpdateAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        matchRepo
            .Setup(repository => repository.ListByStageForUpdateAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        unitOfWork.Setup(uow => uow.ClearTrackedChanges());

        var exclusiveCalls = 0;
        unitOfWork
            .Setup(uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (CompetitionId _, Func<CancellationToken, Task> write, CancellationToken token) =>
            {
                exclusiveCalls++;
                await write(token).ConfigureAwait(false);
                if (simulatePublishCommitFailure && exclusiveCalls == 1)
                {
                    throw new InvalidOperationException("simulated transaction commit failure");
                }
            });

        var executor = new UseCaseExecutor(
            stages.Object,
            matchRepo.Object,
            competitions.Object,
            unitOfWork.Object,
            _clock,
            AlwaysExistingMedia.Instance,
            _logger);

        return (executor, stage.Id, draw.Id);
    }

    private sealed class CollectingLogger : ILogger<UseCaseExecutor>
    {
        public List<(EventId EventId, LogLevel Level)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((eventId, logLevel));
    }
}
