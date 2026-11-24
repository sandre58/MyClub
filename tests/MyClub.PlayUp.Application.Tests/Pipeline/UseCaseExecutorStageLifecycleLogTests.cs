// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorStageLifecycleLogTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;
using DomainMatch = MyClub.PlayUp.Domain.Matches.Match;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

/// <summary>
/// StagePrepared / StageStarted / StageCompleted logs are emitted only after exclusive write commit.
/// </summary>
public sealed class UseCaseExecutorStageLifecycleLogTests
{
    private const int StagePreparedEventId = 1010;
    private const int StageStartedEventId = 1011;
    private const int StageCompletedEventId = 1012;

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 15, 0, 0, TimeSpan.Zero));
    private readonly CollectingLogger _logger = new();

    [Theory]
    [InlineData(StagePreparedEventId, "Prepare")]
    [InlineData(StageStartedEventId, "Start")]
    [InlineData(StageCompletedEventId, "Complete")]
    public async Task Stage_lifecycle_logs_after_exclusive_write_completesAsync(int eventId, string operation)
    {
        var (executor, stageId) = CreateExecutorForSuccessfulLifecycle(operation, simulateCommitFailure: false);

        await InvokeLifecycleAsync(executor, stageId, operation);

        _logger.Entries.Should().ContainSingle(e => e.EventId == eventId);
    }

    [Theory]
    [InlineData("Prepare")]
    [InlineData("Start")]
    [InlineData("Complete")]
    public async Task Stage_lifecycle_does_not_log_when_exclusive_write_fails_after_saveAsync(string operation)
    {
        var (executor, stageId) = CreateExecutorForSuccessfulLifecycle(operation, simulateCommitFailure: true);

        var act = async () => await InvokeLifecycleAsync(executor, stageId, operation);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _logger.Entries.Should().BeEmpty();
    }

    private static Task InvokeLifecycleAsync(UseCaseExecutor executor, StageId stageId, string operation) =>
        operation switch
        {
            "Prepare" => executor.PrepareStageAsync(stageId),
            "Start" => executor.StartStageAsync(stageId),
            "Complete" => executor.CompleteStageAsync(stageId),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };

    private (UseCaseExecutor Executor, StageId StageId) CreateExecutorForSuccessfulLifecycle(
        string operation,
        bool simulateCommitFailure)
    {
        var competition = Competition.Create(new CompetitionName("Stage Log"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "H", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD1"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);

        IReadOnlyList<DomainMatch> stageMatches = [];
        switch (operation)
        {
            case "Start":
                {
                    StageMaterializationTestSupport.EnsureReadyForMaterialize(competition, stage, [stage], _clock);
                    var materialized = MaterializeMatches.Execute(competition, stage, [], _clock, [stage]);
                    stageMatches = materialized.CreatedMatches;
                    stage.Status.Should().Be(StageStatus.Ready);
                    break;
                }

            case "Complete":
                {
                    PrepareStage.Execute(stage, [stage], _clock);
                    StartStage.Execute(stage, _clock);
                    var home = competition.Entries[0].Id;
                    var away = competition.Entries[1].Id;
                    var match = DomainMatch.Create(competition.Id, stage.Id, home, away, _clock);
                    match.Start(_clock);
                    match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
                    stageMatches = [match];
                    break;
                }
        }

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
        stages
            .Setup(repository => repository.GetByIdsForUpdateAsync(
                It.Is<IReadOnlyList<StageId>>(ids => ids.SequenceEqual(competition.StageIds)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([stage]);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        matchRepo
            .Setup(repository => repository.ListByStageForUpdateAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stageMatches);
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

        return (executor, stage.Id);
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
