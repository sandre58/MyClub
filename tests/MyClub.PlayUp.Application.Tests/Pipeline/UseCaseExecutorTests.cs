// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Moq;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;
using DomainMatch = MyClub.PlayUp.Domain.Matches.Match;
using DomainMatchResult = MyClub.PlayUp.Domain.Matches.MatchResult;
using DomainScore = MyClub.PlayUp.Domain.Matches.Score;
using MatchErrorCodes = MyClub.PlayUp.Domain.Matches.MatchErrorCodes;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

public sealed class UseCaseExecutorTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 17, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task PrepareStageAsync_loads_executes_and_saves_onceAsync()
    {
        var scenario = CreateDraftChampionshipOnCompetition();
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdAsync(scenario.Stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Stage);
        competitions
            .Setup(repository => repository.GetByIdAsync(scenario.Competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Competition);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        await executor.PrepareStageAsync(scenario.Stage.Id);

        scenario.Stage.Status.Should().Be(StageStatus.Ready);
        stages.Verify(repository => repository.GetByIdAsync(scenario.Stage.Id, It.IsAny<CancellationToken>()), Times.Exactly(2));
        competitions.Verify(
            repository => repository.GetByIdAsync(scenario.Competition.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PrepareStageAsync_when_missing_throws_and_does_not_saveAsync()
    {
        var stageId = StageId.New();
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stage?)null);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var act = async () => await executor.PrepareStageAsync(stageId);

        var exception = await act.Should().ThrowAsync<ApplicationFailureException>();
        exception.Which.Code.Should().Be(ApplicationErrorCodes.StageNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PrepareStageAsync_when_domain_rejects_does_not_saveAsync()
    {
        var scenario = CreateDraftChampionshipOnCompetition();
        PrepareStage.Execute(scenario.Stage, [scenario.Stage], _clock);
        scenario.Stage.Status.Should().Be(StageStatus.Ready);

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdAsync(scenario.Stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Stage);
        competitions
            .Setup(repository => repository.GetByIdAsync(scenario.Competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Competition);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var act = async () => await executor.PrepareStageAsync(scenario.Stage.Id);

        var exception = await act.Should().ThrowAsync<DomainException>();
        exception.Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartStageAsync_loads_starts_and_saves_onceAsync()
    {
        var scenario = CreateDraftChampionshipOnCompetition();
        PrepareStage.Execute(scenario.Stage, [scenario.Stage], _clock);
        scenario.Stage.Status.Should().Be(StageStatus.Ready);

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdAsync(scenario.Stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Stage);
        SetupCompetitionLookup(competitions, scenario.Competition);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        await executor.StartStageAsync(scenario.Stage.Id);

        scenario.Stage.Status.Should().Be(StageStatus.Running);
        stages.Verify(repository => repository.GetByIdAsync(scenario.Stage.Id, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartStageAsync_when_missing_throws_and_does_not_saveAsync()
    {
        var stageId = StageId.New();
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stage?)null);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var act = async () => await executor.StartStageAsync(stageId);

        var exception = await act.Should().ThrowAsync<ApplicationFailureException>();
        exception.Which.Code.Should().Be(ApplicationErrorCodes.StageNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartStageAsync_when_domain_rejects_does_not_saveAsync()
    {
        var scenario = CreateDraftChampionshipOnCompetition();

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdAsync(scenario.Stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Stage);
        SetupCompetitionLookup(competitions, scenario.Competition);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var act = async () => await executor.StartStageAsync(scenario.Stage.Id);

        var exception = await act.Should().ThrowAsync<DomainException>();
        exception.Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PrepareStageAsync_loads_multi_stage_for_cross_stage_progressionAsync()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var quarter = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        quarter.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        var fixture = quarter.AddFixture(quarter.Rounds[0].Id, _clock);

        var semi = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        semi.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        semi.AddSlot("SF1-A", _clock);

        competition.AddStage(quarter.Id, _clock);
        competition.AddStage(semi.Id, _clock);

        quarter.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(semi.Id, "SF1-A"))
            ]),
            _clock);

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages.Setup(repository => repository.GetByIdAsync(quarter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quarter);
        stages.Setup(repository => repository.GetByIdAsync(semi.Id, It.IsAny<CancellationToken>())).ReturnsAsync(semi);
        competitions
            .Setup(repository => repository.GetByIdAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        await executor.PrepareStageAsync(quarter.Id);

        quarter.Status.Should().Be(StageStatus.Ready);
        stages.Verify(repository => repository.GetByIdAsync(quarter.Id, It.IsAny<CancellationToken>()), Times.Exactly(2));
        stages.Verify(repository => repository.GetByIdAsync(semi.Id, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyProgressionOutcomeAsync_loads_multi_ar_executes_and_saves_onceAsync()
    {
        var scenario = CreateCupProgressionScenario();
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdAsync(scenario.Source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Source);
        stages
            .Setup(repository => repository.GetByIdAsync(scenario.Destination.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Destination);
        competitions
            .Setup(repository => repository.GetByIdAsync(scenario.Competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Competition);
        matchRepo
            .Setup(repository => repository.GetByIdAsync(scenario.Match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Match);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = CreateExecutor(stages, matchRepo, competitions, unitOfWork);
        await executor.ApplyProgressionOutcomeAsync(scenario.Source.Id, scenario.FixtureId);

        scenario.Destination.FindSlot("SF1-A")!.EntryId.Should().Be(scenario.Home);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        matchRepo.Verify(repository => repository.GetByIdAsync(scenario.Match.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyProgressionOutcomeAsync_when_match_missing_does_not_saveAsync()
    {
        var scenario = CreateCupProgressionScenario();
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdAsync(scenario.Source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Source);
        stages
            .Setup(repository => repository.GetByIdAsync(scenario.Destination.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Destination);
        competitions
            .Setup(repository => repository.GetByIdAsync(scenario.Competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Competition);
        matchRepo
            .Setup(repository => repository.GetByIdAsync(scenario.Match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainMatch?)null);

        var executor = CreateExecutor(stages, matchRepo, competitions, unitOfWork);
        var act = async () => await executor.ApplyProgressionOutcomeAsync(scenario.Source.Id, scenario.FixtureId);

        var exception = await act.Should().ThrowAsync<ApplicationFailureException>();
        exception.Which.Code.Should().Be(ApplicationErrorCodes.MatchNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishDrawAsync_loads_publishes_and_saves_onceAsync()
    {
        var (competition, stage, drawId) = CreateReadyToPublishSlotDraw();
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);
        SetupCompetitionLookup(competitions, competition);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        await executor.PublishDrawAsync(stage.Id, drawId);

        stage.GetDraw(drawId).Status.Should().Be(DrawStatus.Published);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishDrawAsync_when_stage_missing_does_not_saveAsync()
    {
        var stageId = StageId.New();
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stage?)null);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var act = async () => await executor.PublishDrawAsync(stageId, DrawId.New());

        var exception = await act.Should().ThrowAsync<ApplicationFailureException>();
        exception.Which.Code.Should().Be(ApplicationErrorCodes.StageNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishDrawAsync_when_domain_rejects_does_not_saveAsync()
    {
        var scenario = CreateDraftChampionshipOnCompetition();
        var draw = scenario.Stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        scenario.Stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([EntryId.New()]), _clock);

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdAsync(scenario.Stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Stage);
        SetupCompetitionLookup(competitions, scenario.Competition);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var act = async () => await executor.PublishDrawAsync(scenario.Stage.Id, draw.Id);

        var exception = await act.Should().ThrowAsync<DomainException>();
        exception.Which.Code.Should().Be(StageErrorCodes.DrawInvalidTransition);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartMatchAsync_loads_starts_and_saves_onceAsync()
    {
        var competition = CreateOpenCompetition();
        var match = DomainMatch.Create(competition.Id, StageId.New(), EntryId.New(), EntryId.New(), _clock);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        matchRepo
            .Setup(repository => repository.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);
        SetupCompetitionLookup(competitions, competition);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = CreateExecutor(stages, matchRepo, competitions, unitOfWork);
        await executor.StartMatchAsync(match.Id);

        match.Status.Should().Be(MatchStatus.Live);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartMatchAsync_when_missing_does_not_saveAsync()
    {
        var matchId = MatchId.New();
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        matchRepo
            .Setup(repository => repository.GetByIdAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainMatch?)null);

        var executor = CreateExecutor(stages, matchRepo, competitions, unitOfWork);
        var act = async () => await executor.StartMatchAsync(matchId);

        var exception = await act.Should().ThrowAsync<ApplicationFailureException>();
        exception.Which.Code.Should().Be(ApplicationErrorCodes.MatchNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartMatchAsync_when_domain_rejects_does_not_saveAsync()
    {
        var competition = CreateOpenCompetition();
        var match = DomainMatch.Create(competition.Id, StageId.New(), EntryId.New(), EntryId.New(), _clock);
        match.Start(_clock);

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        matchRepo
            .Setup(repository => repository.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);
        SetupCompetitionLookup(competitions, competition);

        var executor = CreateExecutor(stages, matchRepo, competitions, unitOfWork);
        var act = async () => await executor.StartMatchAsync(match.Id);

        var exception = await act.Should().ThrowAsync<DomainException>();
        exception.Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FinishMatchAsync_loads_finishes_and_saves_onceAsync()
    {
        var competition = CreateOpenCompetition();
        var match = DomainMatch.Create(competition.Id, StageId.New(), EntryId.New(), EntryId.New(), _clock);
        match.Start(_clock);
        var result = new DomainMatchResult(ResultType.Played, new DomainScore(2, 1));

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        matchRepo
            .Setup(repository => repository.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);
        SetupCompetitionLookup(competitions, competition);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = CreateExecutor(stages, matchRepo, competitions, unitOfWork);
        await executor.FinishMatchAsync(match.Id, result);

        match.Status.Should().Be(MatchStatus.Finished);
        match.Result.Should().Be(result);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FinishMatchAsync_when_missing_does_not_saveAsync()
    {
        var matchId = MatchId.New();
        var result = new DomainMatchResult(ResultType.Played, new DomainScore(1, 0));
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        matchRepo
            .Setup(repository => repository.GetByIdAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainMatch?)null);

        var executor = CreateExecutor(stages, matchRepo, competitions, unitOfWork);
        var act = async () => await executor.FinishMatchAsync(matchId, result);

        var exception = await act.Should().ThrowAsync<ApplicationFailureException>();
        exception.Which.Code.Should().Be(ApplicationErrorCodes.MatchNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FinishMatchAsync_when_domain_rejects_does_not_saveAsync()
    {
        var competition = CreateOpenCompetition();
        var match = DomainMatch.Create(competition.Id, StageId.New(), EntryId.New(), EntryId.New(), _clock);
        match.Cancel(_clock);
        var result = new DomainMatchResult(ResultType.Played, new DomainScore(1, 0));

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        matchRepo
            .Setup(repository => repository.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);
        SetupCompetitionLookup(competitions, competition);

        var executor = CreateExecutor(stages, matchRepo, competitions, unitOfWork);
        var act = async () => await executor.FinishMatchAsync(match.Id, result);

        var exception = await act.Should().ThrowAsync<DomainException>();
        exception.Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(CompetitionStatus.Completed)]
    [InlineData(CompetitionStatus.Archived)]
    public async Task StartMatchAsync_when_competition_closed_rejects_without_saveAsync(CompetitionStatus closedStatus)
    {
        var competition = CreateClosedCompetition(closedStatus);
        var match = DomainMatch.Create(competition.Id, StageId.New(), EntryId.New(), EntryId.New(), _clock);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        matchRepo
            .Setup(repository => repository.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);
        SetupCompetitionLookup(competitions, competition);

        var executor = CreateExecutor(stages, matchRepo, competitions, unitOfWork);
        var act = async () => await executor.StartMatchAsync(match.Id);

        var exception = await act.Should().ThrowAsync<ApplicationFailureException>();
        exception.Which.Code.Should().Be(ApplicationErrorCodes.MatchOperationNotAllowed);
        match.Status.Should().Be(MatchStatus.Scheduled);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(CompetitionStatus.Completed)]
    [InlineData(CompetitionStatus.Archived)]
    public async Task FinishMatchAsync_when_competition_closed_rejects_without_saveAsync(CompetitionStatus closedStatus)
    {
        var competition = CreateClosedCompetition(closedStatus);
        var match = DomainMatch.Create(competition.Id, StageId.New(), EntryId.New(), EntryId.New(), _clock);
        match.Start(_clock);
        var result = new DomainMatchResult(ResultType.Played, new DomainScore(1, 0));

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        matchRepo
            .Setup(repository => repository.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);
        SetupCompetitionLookup(competitions, competition);

        var executor = CreateExecutor(stages, matchRepo, competitions, unitOfWork);
        var act = async () => await executor.FinishMatchAsync(match.Id, result);

        var exception = await act.Should().ThrowAsync<ApplicationFailureException>();
        exception.Which.Code.Should().Be(ApplicationErrorCodes.MatchOperationNotAllowed);
        match.Status.Should().Be(MatchStatus.Live);
        match.Result.Should().BeNull();
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(CompetitionStatus.Draft)]
    [InlineData(CompetitionStatus.Ready)]
    [InlineData(CompetitionStatus.Running)]
    public async Task StartMatchAsync_when_competition_open_allows_startAsync(CompetitionStatus openStatus)
    {
        var competition = CreateCompetitionAt(openStatus);
        var match = DomainMatch.Create(competition.Id, StageId.New(), EntryId.New(), EntryId.New(), _clock);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        matchRepo
            .Setup(repository => repository.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);
        SetupCompetitionLookup(competitions, competition);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = CreateExecutor(stages, matchRepo, competitions, unitOfWork);
        await executor.StartMatchAsync(match.Id);

        match.Status.Should().Be(MatchStatus.Live);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyDrawAsync_pairing_creates_match_adds_and_saves_onceAsync()
    {
        var scenario = CreatePublishedPairingDraw();
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdAsync(scenario.Stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Stage);
        SetupCompetitionLookup(competitions, scenario.Competition);
        matchRepo.Setup(repository => repository.Add(It.IsAny<DomainMatch>()));
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = CreateExecutor(stages, matchRepo, competitions, unitOfWork);
        await executor.ApplyDrawAsync(scenario.Stage.Id, scenario.DrawId, [scenario.FixtureId]);

        scenario.Stage.GetFixture(scenario.FixtureId).MatchIds.Should().ContainSingle();
        matchRepo.Verify(repository => repository.Add(It.IsAny<DomainMatch>()), Times.Once);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyDrawAsync_when_stage_missing_does_not_saveAsync()
    {
        var stageId = StageId.New();
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stage?)null);

        var executor = CreateExecutor(stages, matchRepo, competitions, unitOfWork);
        var act = async () => await executor.ApplyDrawAsync(stageId, DrawId.New(), [FixtureId.New()]);

        var exception = await act.Should().ThrowAsync<ApplicationFailureException>();
        exception.Which.Code.Should().Be(ApplicationErrorCodes.StageNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        matchRepo.Verify(repository => repository.Add(It.IsAny<DomainMatch>()), Times.Never);
    }

    [Fact]
    public async Task ApplyDrawAsync_when_draw_not_published_does_not_saveAsync()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var round = stage.AddRound("R1", _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Pairing, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForPairing([EntryId.New(), EntryId.New()]), _clock);

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);
        SetupCompetitionLookup(competitions, competition);

        var executor = CreateExecutor(stages, matchRepo, competitions, unitOfWork);
        var act = async () => await executor.ApplyDrawAsync(stage.Id, draw.Id, [fixture.Id]);

        var exception = await act.Should().ThrowAsync<ApplicationFailureException>();
        exception.Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        matchRepo.Verify(repository => repository.Add(It.IsAny<DomainMatch>()), Times.Never);
        fixture.MatchIds.Should().BeEmpty();
    }

    private static void SetupCompetitionLookup(Mock<ICompetitionRepository> competitions, Competition competition) =>
        competitions
            .Setup(repository => repository.GetByIdAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);

    private UseCaseExecutor CreateExecutor(
        Mock<IStageRepository> stages,
        Mock<IMatchRepository> matches,
        Mock<ICompetitionRepository> competitions,
        Mock<IUnitOfWork> unitOfWork) =>
        new(stages.Object, matches.Object, competitions.Object, unitOfWork.Object, _clock, AlwaysExistingMedia.Instance);

    private Competition CreateOpenCompetition() => CreateCompetitionAt(CompetitionStatus.Draft);

    private Competition CreateClosedCompetition(CompetitionStatus closedStatus) =>
        CreateCompetitionAt(closedStatus);

    private Competition CreateCompetitionAt(CompetitionStatus status)
    {
        var competition = Competition.Create(new CompetitionName("Match Ops"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Team A", _clock);
        competition.AddStage(StageId.New(), _clock);

        if (status == CompetitionStatus.Draft)
        {
            return competition;
        }

        competition.Prepare(_clock);
        if (status == CompetitionStatus.Ready)
        {
            return competition;
        }

        competition.Start(_clock);
        if (status == CompetitionStatus.Running)
        {
            return competition;
        }

        competition.Complete(CompletionMode.Administrative, _clock);
        switch (status)
        {
            case CompetitionStatus.Completed:
                return competition;
            case CompetitionStatus.Archived:
                competition.Archive(_clock);
                return competition;
            case CompetitionStatus.Draft:
            case CompetitionStatus.Ready:
            case CompetitionStatus.Running:
            case CompetitionStatus.Suspended:
            default:
                throw new InvalidOperationException($"Unsupported competition status '{status}' for test setup.");
        }
    }

    private ChampionshipScenario CreateDraftChampionshipOnCompetition()
    {
        var competition = Competition.Create(new CompetitionName("U15 League"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        return new ChampionshipScenario(competition, stage);
    }

    private (Competition Competition, Stage Stage, DrawId DrawId) CreateReadyToPublishSlotDraw()
    {
        var scenario = CreateDraftChampionshipOnCompetition();
        scenario.Stage.AddSlot("A", _clock);
        var entry = EntryId.New();
        var draw = scenario.Stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        scenario.Stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]), _clock);
        scenario.Stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry, "A")]),
            _clock);
        return (scenario.Competition, scenario.Stage, draw.Id);
    }

    private PublishedPairingScenario CreatePublishedPairingDraw()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var round = stage.AddRound("R1", _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        var entryA = EntryId.New();
        var entryB = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Pairing, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForPairing([entryA, entryB]), _clock);
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedPairings([new PairingDrawResult(entryA, entryB)]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        return new PublishedPairingScenario(competition, stage, draw.Id, fixture.Id, entryA, entryB);
    }

    private CupProgressionScenario CreateCupProgressionScenario()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var source = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        source.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        source.AddSlot("QF1-A", _clock);
        source.AddSlot("QF1-B", _clock);

        var destination = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        destination.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        destination.AddSlot("SF1-A", _clock);
        destination.AddSlot("SF1-B", _clock);

        competition.AddStage(source.Id, _clock);
        competition.AddStage(destination.Id, _clock);

        var home = EntryId.New();
        var away = EntryId.New();
        var fixture = source.AddFixture(source.Rounds[0].Id, _clock);
        var match = DomainMatch.Create(competition.Id, source.Id, home, away, _clock);
        source.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        match.Start(_clock);
        match.Finish(new DomainMatchResult(ResultType.Played, new DomainScore(2, 0)), _clock);

        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(destination.Id, "SF1-A"))
            ]),
            _clock);

        return new CupProgressionScenario(competition, source, destination, fixture.Id, match, home);
    }

    private sealed record ChampionshipScenario(Competition Competition, Stage Stage);

    private sealed record CupProgressionScenario(
        Competition Competition,
        Stage Source,
        Stage Destination,
        FixtureId FixtureId,
        DomainMatch Match,
        EntryId Home);

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Test")]
    private sealed record PublishedPairingScenario(
        Competition Competition,
        Stage Stage,
        DrawId DrawId,
        FixtureId FixtureId,
        EntryId EntryA,
        EntryId EntryB);
}
