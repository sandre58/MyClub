// -----------------------------------------------------------------------
// <copyright file="MatchLifecycleStagePhaseExecutorTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;
using DomainMatch = MyClub.PlayUp.Domain.Matches.Match;
using DomainMatchResult = MyClub.PlayUp.Domain.Matches.MatchResult;
using DomainScore = MyClub.PlayUp.Domain.Matches.Score;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

public sealed class MatchLifecycleStagePhaseExecutorTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 12, 12, 0, 0, TimeSpan.Zero));

    [Theory]
    [InlineData(StageStatus.Draft)]
    [InlineData(StageStatus.Ready)]
    public async Task StartMatchAsync_when_stage_not_open_rejects_without_saveAsync(StageStatus stageStatus)
    {
        var (match, stage, competition) = CreateScheduledMatchOnStage(stageStatus);
        var (executor, unitOfWork) = CreateExecutor(match, stage, competition);

        var act = async () => await executor.StartMatchAsync(match.Id);

        var exception = await act.Should().ThrowAsync<ApplicationFailureException>();
        exception.Which.Code.Should().Be(ApplicationErrorCodes.StageExploitationNotOpen);
        match.Status.Should().Be(MatchStatus.Scheduled);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(StageStatus.Running)]
    [InlineData(StageStatus.Suspended)]
    public async Task StartMatchAsync_when_stage_open_starts_and_saves_onceAsync(StageStatus stageStatus)
    {
        var (match, stage, competition) = CreateScheduledMatchOnStage(stageStatus);
        var (executor, unitOfWork) = CreateExecutor(match, stage, competition);

        await executor.StartMatchAsync(match.Id);

        match.Status.Should().Be(MatchStatus.Live);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartMatchAsync_when_stage_completed_rejects_without_saveAsync()
    {
        var (match, stage, competition) = CreateScheduledMatchOnStage(StageStatus.Completed);
        var (executor, unitOfWork) = CreateExecutor(match, stage, competition);

        var act = async () => await executor.StartMatchAsync(match.Id);

        var exception = await act.Should().ThrowAsync<ApplicationFailureException>();
        exception.Which.Code.Should().Be(ApplicationErrorCodes.StageExploitationClosed);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(StageStatus.Draft)]
    [InlineData(StageStatus.Ready)]
    public async Task FinishMatchAsync_when_stage_not_open_rejects_without_saveAsync(StageStatus stageStatus)
    {
        var (match, stage, competition) = CreateLiveMatchOnStage(stageStatus);
        var result = new DomainMatchResult(ResultType.Played, new DomainScore(1, 0));
        var (executor, unitOfWork) = CreateExecutor(match, stage, competition);

        var act = async () => await executor.FinishMatchAsync(match.Id, result);

        var exception = await act.Should().ThrowAsync<ApplicationFailureException>();
        exception.Which.Code.Should().Be(ApplicationErrorCodes.StageExploitationNotOpen);
        match.Status.Should().Be(MatchStatus.Live);
        match.Result.Should().BeNull();
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(StageStatus.Running)]
    [InlineData(StageStatus.Suspended)]
    public async Task FinishMatchAsync_when_stage_open_finishes_and_saves_onceAsync(StageStatus stageStatus)
    {
        var (match, stage, competition) = CreateLiveMatchOnStage(stageStatus);
        var result = new DomainMatchResult(ResultType.Played, new DomainScore(2, 1));
        var (executor, unitOfWork) = CreateExecutor(match, stage, competition);

        await executor.FinishMatchAsync(match.Id, result);

        match.Status.Should().Be(MatchStatus.Finished);
        match.Result.Should().Be(result);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FinishMatchAsync_when_stage_completed_rejects_without_saveAsync()
    {
        var (match, stage, competition) = CreateLiveMatchOnStage(StageStatus.Completed);
        var result = new DomainMatchResult(ResultType.Played, new DomainScore(1, 0));
        var (executor, unitOfWork) = CreateExecutor(match, stage, competition);

        var act = async () => await executor.FinishMatchAsync(match.Id, result);

        var exception = await act.Should().ThrowAsync<ApplicationFailureException>();
        exception.Which.Code.Should().Be(ApplicationErrorCodes.StageExploitationClosed);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private (DomainMatch Match, Stage Stage, Competition Competition) CreateScheduledMatchOnStage(StageStatus targetStatus)
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD1"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        AdvanceStageTo(stage, targetStatus);

        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var match = DomainMatch.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        return (match, stage, competition);
    }

    private (DomainMatch Match, Stage Stage, Competition Competition) CreateLiveMatchOnStage(StageStatus targetStatus)
    {
        if (targetStatus is StageStatus.Draft or StageStatus.Ready)
        {
            var draftOrReady = CreateScheduledMatchOnStage(targetStatus);
            draftOrReady.Match.Start(_clock);
            return draftOrReady;
        }

        if (targetStatus == StageStatus.Completed)
        {
            var running = CreateScheduledMatchOnStage(StageStatus.Running);
            running.Match.Start(_clock);
            running.Stage.Complete(_clock);
            return running;
        }

        var open = CreateScheduledMatchOnStage(targetStatus);
        open.Match.Start(_clock);
        return open;
    }

    private void AdvanceStageTo(Stage stage, StageStatus targetStatus)
    {
        if (targetStatus == StageStatus.Draft)
        {
            return;
        }

        stage.Prepare(_clock);
        if (targetStatus == StageStatus.Ready)
        {
            return;
        }

        stage.Start(_clock);
        if (targetStatus == StageStatus.Running)
        {
            return;
        }

        if (targetStatus == StageStatus.Suspended)
        {
            stage.Suspend(_clock);
            return;
        }

        if (targetStatus == StageStatus.Completed)
        {
            stage.Complete(_clock);
        }
    }

    private (UseCaseExecutor Executor, Mock<IUnitOfWork> UnitOfWork) CreateExecutor(
        DomainMatch match,
        Stage stage,
        Competition competition)
    {
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

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

        var executor = new UseCaseExecutor(
            stages.Object,
            matchRepo.Object,
            competitions.Object,
            unitOfWork.Object,
            _clock,
            AlwaysExistingMedia.Instance,
            NullLogger<UseCaseExecutor>.Instance);

        return (executor, unitOfWork);
    }
}
