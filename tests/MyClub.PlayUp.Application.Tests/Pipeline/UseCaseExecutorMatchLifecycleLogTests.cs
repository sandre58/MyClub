// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorMatchLifecycleLogTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using Xunit;
using DomainMatch = MyClub.PlayUp.Domain.Matches.Match;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

/// <summary>
/// MatchStarted / MatchFinished logs are emitted only after exclusive write commit (Nit 1).
/// </summary>
public sealed class UseCaseExecutorMatchLifecycleLogTests
{
    private const int MatchStartedEventId = 1030;

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 14, 0, 0, TimeSpan.Zero));
    private readonly CollectingLogger _logger = new();

    [Fact]
    public async Task StartMatchAsync_logs_after_exclusive_write_completesAsync()
    {
        var (executor, match) = CreateExecutorForSuccessfulStart();

        await executor.StartMatchAsync(match.Id);

        _logger.Entries.Should().ContainSingle(e => e.EventId == MatchStartedEventId);
    }

    [Fact]
    public async Task StartMatchAsync_does_not_log_when_exclusive_write_fails_after_saveAsync()
    {
        var (executor, match) = CreateExecutorForSuccessfulStart(simulateCommitFailure: true);

        var act = async () => await executor.StartMatchAsync(match.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _logger.Entries.Should().BeEmpty();
    }

    private (UseCaseExecutor Executor, DomainMatch Match) CreateExecutorForSuccessfulStart(
        bool simulateCommitFailure = false)
    {
        var competition = CreateOpenCompetition();
        var stage = CreateRunningStage(competition.Id);
        var match = DomainMatch.Create(competition.Id, stage.Id, EntryId.New(), EntryId.New(), _clock);

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        matchRepo
            .Setup(repository => repository.GetCompetitionIdReadOnlyAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition.Id);
        matchRepo
            .Setup(repository => repository.GetByIdForUpdateAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        stages
            .Setup(repository => repository.GetByIdForUpdateAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);
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

        return (executor, match);
    }

    private Competition CreateOpenCompetition()
    {
        var competition = Competition.Create(new CompetitionName("Open"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Team A", _clock);
        competition.AddStage(StageId.New(), _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        return competition;
    }

    private Stage CreateRunningStage(CompetitionId competitionId)
    {
        var stage = Stage.Create(competitionId, new StageName("MD1"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        return stage;
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
