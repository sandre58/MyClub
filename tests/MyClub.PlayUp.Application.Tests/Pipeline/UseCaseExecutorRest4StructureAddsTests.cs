// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorRest4StructureAddsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

/// <summary>
/// A10-REST-4 — structure add / rename / reorder writes under competition lock envelopes.
/// </summary>
public sealed class UseCaseExecutorRest4StructureAddsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 18, 0, 0, TimeSpan.Zero));

    private static void SetupExclusive(Mock<IUnitOfWork> unitOfWork) =>
        unitOfWork
            .Setup(uow => uow.ExecuteExclusiveAsync(
                It.IsAny<CompetitionId>(),
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns((CompetitionId _, Func<CancellationToken, Task> write, CancellationToken token) => write(token));

    private static void SetupCompetitionId(Mock<IStageRepository> stages, StageId stageId, CompetitionId? competitionId) =>
        stages
            .Setup(repository => repository.GetCompetitionIdReadOnlyAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competitionId);

    [Fact]
    public async Task AddStageRoundAsync_saves_once_under_stage_competition_lockAsync()
    {
        var scenario = CreateDraftCupStage();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories(scenario);
        SetupStageEnvelope(stages, unitOfWork, scenario);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(scenario.Competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Competition);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var round = await executor.AddStageRoundAsync(scenario.Stage.Id, "R2", numberOfLegs: null, aggregateScoring: null);

        round.Name.Should().Be("R2");
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                scenario.Competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddStageRoundAsync_when_stage_missing_throws_without_lock_or_saveAsync()
    {
        var stageId = StageId.New();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupCompetitionId(stages, stageId, null);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var act = () => executor.AddStageRoundAsync(stageId, "R1", null, null);

        (await act.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.StageNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                It.IsAny<CompetitionId>(),
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddStageSlotAsync_saves_once_under_stage_competition_lockAsync()
    {
        var scenario = CreateDraftCupStage();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories(scenario);
        SetupStageEnvelope(stages, unitOfWork, scenario);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(scenario.Competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Competition);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var slot = await executor.AddStageSlotAsync(scenario.Stage.Id, "EXTRA");

        slot.SlotKey.Should().Be("EXTRA");
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddStageSlotAsync_when_running_refuses_without_saveAsync()
    {
        var scenario = CreateDraftCupStage();
        PrepareStage.Execute(scenario.Stage, [scenario.Stage], _clock);
        StartStage.Execute(scenario.Stage, _clock);
        var (stages, matches, competitions, unitOfWork) = CreateRepositories(scenario);
        SetupStageEnvelope(stages, unitOfWork, scenario);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(scenario.Competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Competition);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var act = () => executor.AddStageSlotAsync(scenario.Stage.Id, "EXTRA");

        (await act.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.StructureNotMutable);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddStageMatchdayAsync_saves_once_under_stage_competition_lockAsync()
    {
        var scenario = CreateDraftChampionship();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories(scenario);
        SetupStageEnvelope(stages, unitOfWork, scenario);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(scenario.Competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Competition);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        _ = await executor.AddStageMatchdayAsync(scenario.Stage.Id, 2);

        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddStageMatchdayAsync_when_stage_missing_throws_without_lock_or_saveAsync()
    {
        var stageId = StageId.New();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupCompetitionId(stages, stageId, null);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var act = () => executor.AddStageMatchdayAsync(stageId, 1);

        (await act.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.StageNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddStageGroupAsync_saves_once_under_stage_competition_lockAsync()
    {
        var competition = CreateCompetition.Execute("Groups", _clock);
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 3), _clock).Stage;
        var scenario = new StageScenario(competition, stage);
        var (stages, matches, competitions, unitOfWork) = CreateRepositories(scenario);
        SetupStageEnvelope(stages, unitOfWork, scenario);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var group = await executor.AddStageGroupAsync(stage.Id, "G3");

        group.Name.Should().Be("G3");
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddStageGroupAsync_when_stage_missing_throws_without_lock_or_saveAsync()
    {
        var stageId = StageId.New();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupCompetitionId(stages, stageId, null);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var act = () => executor.AddStageGroupAsync(stageId, "G1");

        (await act.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.StageNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RenameStageAsync_saves_once_under_stage_competition_lockAsync()
    {
        var scenario = CreateDraftChampionship();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories(scenario);
        SetupStageEnvelope(stages, unitOfWork, scenario);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        await executor.RenameStageAsync(scenario.Stage.Id, "Renamed");

        scenario.Stage.Name.Value.Should().Be("Renamed");
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RenameStageAsync_when_completed_refuses_without_saveAsync()
    {
        var scenario = CreateDraftChampionship();
        PrepareStage.Execute(scenario.Stage, [scenario.Stage], _clock);
        StartStage.Execute(scenario.Stage, _clock);
        CompleteStage.Execute(scenario.Stage, [], _clock);
        var (stages, matches, competitions, unitOfWork) = CreateRepositories(scenario);
        SetupStageEnvelope(stages, unitOfWork, scenario);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var act = () => executor.RenameStageAsync(scenario.Stage.Id, "Blocked");

        var exception = await act.Should().ThrowAsync<DomainException>();
        exception.Which.Code.Should().Be(StageErrorCodes.DisplayNameLocked);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RenameStageAsync_when_stage_missing_throws_without_lock_or_saveAsync()
    {
        var stageId = StageId.New();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupCompetitionId(stages, stageId, null);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var act = () => executor.RenameStageAsync(stageId, "X");

        (await act.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.StageNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddCompetitionStageAsync_adds_saves_once_under_competition_lockAsync()
    {
        var competition = CreateCompetition.Execute("Add stage", _clock);
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupCompetitionEnvelope(competitions, stages, matches, unitOfWork, competition);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var (stage, _) = await executor.AddCompetitionStageAsync(competition.Id, StructureIntent.Championship("Phase"));

        stage.Name.Value.Should().Be("Phase");
        stages.Verify(repository => repository.Add(It.IsAny<Stage>()), Times.Once);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddCompetitionStageAsync_when_competition_missing_throws_without_saveAsync()
    {
        var competitionId = CompetitionId.New();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupExclusive(unitOfWork);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(competitionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Competition?)null);
        SetupStructureReads(matches, stages, competitionId, []);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var act = () => executor.AddCompetitionStageAsync(competitionId, StructureIntent.Championship("X"));

        (await act.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.CompetitionNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        stages.Verify(repository => repository.Add(It.IsAny<Stage>()), Times.Never);
    }

    [Fact]
    public async Task InsertCompetitionStageAsync_adds_saves_once_under_competition_lockAsync()
    {
        var competition = CreateCompetition.Execute("Insert", _clock);
        var anchor = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Anchor"), _clock);
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupCompetitionEnvelope(competitions, stages, matches, unitOfWork, competition, anchor);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var (stage, _) = await executor.InsertCompetitionStageAsync(
            competition.Id,
            StructureIntent.Championship("New"),
            anchor.Id,
            StageInsertPosition.After);

        stage.Name.Value.Should().Be("New");
        stages.Verify(repository => repository.Add(It.IsAny<Stage>()), Times.Once);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetCompetitionStageOrderAsync_saves_once_under_competition_lockAsync()
    {
        var competition = CreateCompetition.Execute("Order", _clock);
        var first = AddCompetitionStage.Execute(competition, StructureIntent.Championship("A"), _clock);
        var second = AddCompetitionStage.Execute(competition, StructureIntent.Championship("B"), _clock);
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupCompetitionEnvelope(competitions, stages, matches, unitOfWork, competition, first, second);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        _ = await executor.SetCompetitionStageOrderAsync(competition.Id, [second.Id, first.Id]);

        competition.StageIds.Should().Equal(second.Id, first.Id);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetCompetitionStageOrderAsync_when_competition_missing_throws_without_saveAsync()
    {
        var competitionId = CompetitionId.New();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupExclusive(unitOfWork);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(competitionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Competition?)null);
        SetupStructureReads(matches, stages, competitionId, []);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var act = () => executor.SetCompetitionStageOrderAsync(competitionId, []);

        (await act.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.CompetitionNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static void SetupStageEnvelope(
        Mock<IStageRepository> stages,
        Mock<IUnitOfWork> unitOfWork,
        StageScenario scenario)
    {
        SetupExclusive(unitOfWork);
        SetupCompetitionId(stages, scenario.Stage.Id, scenario.Competition.Id);
        stages
            .Setup(repository => repository.GetByIdForUpdateAsync(scenario.Stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario.Stage);
    }

    private static void SetupCompetitionEnvelope(
        Mock<ICompetitionRepository> competitions,
        Mock<IStageRepository> stages,
        Mock<IMatchRepository> matches,
        Mock<IUnitOfWork> unitOfWork,
        Competition competition,
        params Stage[] extraStages)
    {
        SetupExclusive(unitOfWork);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        var loadedStages = extraStages.ToList();
        stages
            .Setup(repository => repository.Add(It.IsAny<Stage>()))
            .Callback<Stage>(loadedStages.Add)
            .Verifiable();
        stages
            .Setup(repository => repository.GetByIdsReadOnlyAsync(
                It.IsAny<IReadOnlyList<StageId>>(),
                It.IsAny<StageReadCapabilities>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<StageId> ids, StageReadCapabilities _, CancellationToken _) =>
                [.. loadedStages.Where(stage => ids.Contains(stage.Id))]);
        SetupStructureReads(matches, stages, competition.Id, loadedStages);
    }

    private static void SetupStructureReads(
        Mock<IMatchRepository> matches,
        Mock<IStageRepository> stages,
        CompetitionId competitionId,
        IReadOnlyList<Stage> loadedStages)
    {
        matches
            .Setup(repository => repository.ListSheetMemberRefsByCompetitionReadOnlyAsync(
                competitionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        matches
            .Setup(repository => repository.ListSummaryRowsByStageIdsReadOnlyAsync(
                It.IsAny<IReadOnlyList<StageId>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>());
        stages
            .Setup(repository => repository.GetByIdsReadOnlyAsync(
                It.IsAny<IReadOnlyList<StageId>>(),
                It.IsAny<StageReadCapabilities>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(loadedStages);
    }

    private static (
        Mock<IStageRepository> Stages,
        Mock<IMatchRepository> Matches,
        Mock<ICompetitionRepository> Competitions,
        Mock<IUnitOfWork> UnitOfWork) CreateRepositories(StageScenario? scenario = null)
    {
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        if (scenario is not null)
        {
            SetupStructureReads(matches, stages, scenario.Competition.Id, [scenario.Stage]);
        }

        return (stages, matches, competitions, unitOfWork);
    }

    private UseCaseExecutor CreateExecutor(
        Mock<IStageRepository> stages,
        Mock<IMatchRepository> matches,
        Mock<ICompetitionRepository> competitions,
        Mock<IUnitOfWork> unitOfWork) =>
        new(
            stages.Object,
            matches.Object,
            competitions.Object,
            unitOfWork.Object,
            _clock,
            AlwaysExistingMedia.Instance,
            NullLogger<UseCaseExecutor>.Instance);

    private StageScenario CreateDraftCupStage()
    {
        var competition = CreateCompetition.Execute("Cup add", _clock);
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(4), _clock).Stage;
        return new StageScenario(competition, stage);
    }

    private StageScenario CreateDraftChampionship()
    {
        var competition = CreateCompetition.Execute("Champ add", _clock);
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Championship("Main"), _clock).Stage;
        return new StageScenario(competition, stage);
    }

    private sealed record StageScenario(Competition Competition, Stage Stage);
}
