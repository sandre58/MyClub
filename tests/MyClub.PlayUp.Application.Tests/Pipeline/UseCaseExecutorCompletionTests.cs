// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorCompletionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Moq;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using Xunit;
using DomainMatch = MyClub.PlayUp.Domain.Matches.Match;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

public sealed class UseCaseExecutorCompletionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task CompleteCompetitionAsync_normal_persists_onceAsync()
    {
        var ctx = CreateRunningFinished();
        var (executor, unitOfWork) = CreateExecutor(ctx.Competition, ctx.Stage, [ctx.Match]);

        await executor.CompleteCompetitionAsync(ctx.Competition.Id, CompletionMode.Normal);

        ctx.Competition.Status.Should().Be(CompetitionStatus.Completed);
        ctx.Competition.CompletionMode.Should().Be(CompletionMode.Normal);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteCompetitionAsync_normal_incomplete_does_not_saveAsync()
    {
        var ctx = CreateRunningScheduled();
        var (executor, unitOfWork) = CreateExecutor(ctx.Competition, ctx.Stage, [ctx.Match]);

        var act = () => executor.CompleteCompetitionAsync(ctx.Competition.Id, CompletionMode.Normal);

        await act.Should().ThrowAsync<ApplicationFailureException>()
            .Where(ex => ex.Code == ApplicationErrorCodes.CompletionNotAllowed);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        ctx.Competition.Status.Should().Be(CompetitionStatus.Running);
    }

    [Fact]
    public async Task ArchiveCompetitionAsync_from_completed_persists_onceAsync()
    {
        var competition = Competition.Create(new CompetitionName("Done"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        var stage = Stage.Create(competition.Id, new StageName("S"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        competition.Complete(CompletionMode.Administrative, _clock);
        var (executor, unitOfWork) = CreateExecutor(competition, stage, matches: []);

        await executor.ArchiveCompetitionAsync(competition.Id);

        competition.Status.Should().Be(CompetitionStatus.Archived);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyScheduleAsync_when_completed_returns_competition_closedAsync()
    {
        var ctx = CreateRunningFinished();
        ctx.Competition.Complete(CompletionMode.Administrative, _clock);
        var (executor, unitOfWork) = CreateExecutor(ctx.Competition, ctx.Stage, [ctx.Match]);

        var act = () => executor.ApplyScheduleAsync(
            ctx.Stage.Id,
            [new ScheduleAssignmentDto(ctx.Match.Id.Value, _clock.UtcNow, Guid.NewGuid())],
            [ctx.Match.Id.Value]);

        await act.Should().ThrowAsync<ApplicationFailureException>()
            .Where(ex => ex.Code == ApplicationErrorCodes.CompetitionClosed);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateDrawAsync_when_completed_returns_competition_closedAsync()
    {
        var ctx = CreateRunningFinished();
        ctx.Competition.Complete(CompletionMode.Administrative, _clock);
        var (executor, unitOfWork) = CreateExecutor(ctx.Competition, ctx.Stage, [ctx.Match]);

        var act = () => executor.CreateDrawAsync(ctx.Stage.Id, DrawResolutionKind.Slot);

        await act.Should().ThrowAsync<ApplicationFailureException>()
            .Where(ex => ex.Code == ApplicationErrorCodes.CompetitionClosed);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetWorkspaceSummaryAsync_exposes_completion_hintsAsync()
    {
        var ctx = CreateRunningFinished();
        var (executor, _) = CreateExecutor(ctx.Competition, ctx.Stage, [ctx.Match]);

        var summary = await executor.GetWorkspaceSummaryAsync(ctx.Competition.Id);

        summary.CanCompleteNormally.Should().BeTrue();
        summary.NextActionCode.Should().Be(WorkspaceSummaryAssembler.CompleteCompetitionCode);
        summary.CompletionBlockers.Should().BeEmpty();
    }

    private (UseCaseExecutor Executor, Mock<IUnitOfWork> UnitOfWork) CreateExecutor(
        Competition competition,
        Stage? stage,
        IReadOnlyList<DomainMatch> matches)
    {
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        competitions
            .Setup(repository => repository.GetByIdAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        if (stage is not null)
        {
            stages
                .Setup(repository => repository.GetByIdAsync(stage.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stage);
        }

        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        if (stage is not null)
        {
            matchRepo
                .Setup(repository => repository.ListByStageAsync(stage.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(matches);
        }

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = new UseCaseExecutor(
            stages.Object,
            matchRepo.Object,
            competitions.Object,
            unitOfWork.Object,
            _clock);
        return (executor, unitOfWork);
    }

    private (Competition Competition, Stage Stage, DomainMatch Match) CreateRunningFinished()
    {
        var ctx = CreateRunningScheduled();
        ctx.Match.Start(_clock);
        ctx.Match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        return ctx;
    }

    private (Competition Competition, Stage Stage, DomainMatch Match) CreateRunningScheduled()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "A", _clock);
        var away = competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        var match = DomainMatch.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        return (competition, stage, match);
    }
}
