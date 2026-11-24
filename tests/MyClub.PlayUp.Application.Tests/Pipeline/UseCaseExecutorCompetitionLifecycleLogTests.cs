// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorCompetitionLifecycleLogTests.cs" company="Stéphane ANDRE">
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
using DomainMatch = MyClub.PlayUp.Domain.Matches.Match;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

/// <summary>
/// Competition lifecycle logs are emitted only after exclusive write commit (A10-REST-6).
/// </summary>
public sealed class UseCaseExecutorCompetitionLifecycleLogTests
{
    private const int CompetitionPreparedEventId = 1002;
    private const int CompetitionStartedEventId = 1003;
    private const int CompetitionCompletedEventId = 1004;
    private const int CompetitionArchivedEventId = 1005;
    private const int CompetitionSuspendedEventId = 1006;
    private const int CompetitionResumedEventId = 1007;

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 18, 15, 0, TimeSpan.Zero));
    private readonly CollectingLogger _logger = new();

    [Theory]
    [InlineData(CompetitionPreparedEventId, "Prepare")]
    [InlineData(CompetitionStartedEventId, "Start")]
    [InlineData(CompetitionSuspendedEventId, "Suspend")]
    [InlineData(CompetitionResumedEventId, "Resume")]
    [InlineData(CompetitionCompletedEventId, "Complete")]
    [InlineData(CompetitionArchivedEventId, "Archive")]
    public async Task Competition_lifecycle_logs_after_exclusive_write_completesAsync(int eventId, string operation)
    {
        var (executor, competitionId) = CreateExecutorForSuccessfulLifecycle(operation, simulateCommitFailure: false);

        await InvokeLifecycleAsync(executor, competitionId, operation);

        _logger.Entries.Should().ContainSingle(e => e.EventId == eventId);
    }

    [Theory]
    [InlineData("Prepare")]
    [InlineData("Start")]
    [InlineData("Suspend")]
    [InlineData("Resume")]
    [InlineData("Complete")]
    [InlineData("Archive")]
    public async Task Competition_lifecycle_does_not_log_when_exclusive_write_fails_after_saveAsync(string operation)
    {
        var (executor, competitionId) = CreateExecutorForSuccessfulLifecycle(operation, simulateCommitFailure: true);

        var act = async () => await InvokeLifecycleAsync(executor, competitionId, operation);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _logger.Entries.Should().BeEmpty();
    }

    private static Task InvokeLifecycleAsync(UseCaseExecutor executor, CompetitionId competitionId, string operation) =>
        operation switch
        {
            "Prepare" => executor.PrepareCompetitionAsync(competitionId),
            "Start" => executor.StartCompetitionAsync(competitionId),
            "Suspend" => executor.SuspendCompetitionAsync(competitionId),
            "Resume" => executor.ResumeCompetitionAsync(competitionId),
            "Complete" => executor.CompleteCompetitionAsync(competitionId, CompletionMode.Administrative),
            "Archive" => executor.ArchiveCompetitionAsync(competitionId),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };

    private (UseCaseExecutor Executor, CompetitionId CompetitionId) CreateExecutorForSuccessfulLifecycle(
        string operation,
        bool simulateCommitFailure)
    {
        var competition = Competition.Create(new CompetitionName("Competition Log"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "H", _clock);
        var away = competition.AddEntry(TeamId.New(), "A", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD1"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);

        switch (operation)
        {
            case "Prepare":
                break;
            case "Start":
                competition.Prepare(_clock);
                break;
            case "Suspend":
                competition.Prepare(_clock);
                competition.Start(_clock);
                break;
            case "Resume":
                competition.Prepare(_clock);
                competition.Start(_clock);
                competition.Suspend(_clock);
                break;
            case "Complete":
                competition.Prepare(_clock);
                competition.Start(_clock);
                break;
            case "Archive":
                competition.Prepare(_clock);
                competition.Start(_clock);
                competition.Complete(CompletionMode.Administrative, _clock);
                break;
        }

        IReadOnlyList<DomainMatch> stageMatches = [];
        if (operation == "Complete")
        {
            var match = DomainMatch.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
            stageMatches = [match];
        }

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        stages
            .Setup(repository => repository.GetByIdsForUpdateAsync(
                It.Is<IReadOnlyList<StageId>>(ids => ids.SequenceEqual(competition.StageIds)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([stage]);
        matchRepo
            .Setup(repository => repository.ListByStageIdsReadOnlyAsync(
                It.Is<IReadOnlyList<StageId>>(ids => ids.Contains(stage.Id)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<StageId, IReadOnlyList<DomainMatch>> { [stage.Id] = stageMatches });

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

        return (executor, competition.Id);
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
