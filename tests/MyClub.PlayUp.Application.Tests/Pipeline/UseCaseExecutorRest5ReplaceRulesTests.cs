// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorRest5ReplaceRulesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

/// <summary>
/// A10-REST-5 — Replace* rules / intents / formats under <see cref="UseCaseExecutor.ExecuteOnStageCompetitionAsync"/>.
/// </summary>
public sealed class UseCaseExecutorRest5ReplaceRulesTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 19, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task ReplaceStageMatchGenerationFormatAsync_saves_once_under_stage_competition_lockAsync()
    {
        var competition = CreateCompetition.Execute("Groups fmt", _clock);
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock).Stage;
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupStageWriteEnvelope(stages, matches, competitions, unitOfWork, competition, stage);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        await executor.ReplaceStageMatchGenerationFormatAsync(stage.Id, MatchGenerationFormat.DoubleRoundRobin);

        stage.MatchGenerationFormat.Should().Be(MatchGenerationFormat.DoubleRoundRobin);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReplaceStageMatchGenerationFormatAsync_when_stage_missing_throws_without_lock_or_saveAsync()
    {
        await AssertStageMissingAsync(stageId => executor =>
            executor.ReplaceStageMatchGenerationFormatAsync(stageId, MatchGenerationFormat.SingleRoundRobin));
    }

    [Fact]
    public async Task ReplaceStageSwissSettingsAsync_saves_once_under_stage_competition_lockAsync()
    {
        var competition = CreateCompetition.Execute("Swiss", _clock);
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Swiss(3), _clock).Stage;
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupStageWriteEnvelope(stages, matches, competitions, unitOfWork, competition, stage);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        await executor.ReplaceStageSwissSettingsAsync(stage.Id, 5);

        stage.SwissSettings!.RoundCount.Should().Be(5);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceStageSwissSettingsAsync_when_stage_missing_throws_without_lock_or_saveAsync()
    {
        await AssertStageMissingAsync(stageId => executor => executor.ReplaceStageSwissSettingsAsync(stageId, 3));
    }

    [Fact]
    public async Task ReplaceStageProgressionRulesAsync_saves_once_under_stage_competition_lockAsync()
    {
        var scenario = CreateDraftCupStage();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupStageWriteEnvelope(stages, matches, competitions, unitOfWork, scenario.Competition, scenario.Stage);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        await executor.ReplaceStageProgressionRulesAsync(scenario.Stage.Id, paths: null);

        scenario.Stage.Regulation.ProgressionRules.Should().BeNull();
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceStageProgressionRulesAsync_when_stage_missing_throws_without_lock_or_saveAsync()
    {
        await AssertStageMissingAsync(stageId => executor =>
            executor.ReplaceStageProgressionRulesAsync(stageId, paths: null));
    }

    [Fact]
    public async Task ReplaceStageProgressionIntentsAsync_saves_once_under_stage_competition_lockAsync()
    {
        var scenario = CreateDraftCupStage();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupStageWriteEnvelope(stages, matches, competitions, unitOfWork, scenario.Competition, scenario.Stage);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        await executor.ReplaceStageProgressionIntentsAsync(scenario.Stage.Id, intents: null);

        scenario.Stage.Regulation.ProgressionRules.Should().BeNull();
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceStageProgressionIntentsAsync_when_stage_missing_throws_without_lock_or_saveAsync()
    {
        await AssertStageMissingAsync(stageId => executor =>
            executor.ReplaceStageProgressionIntentsAsync(stageId, intents: null));
    }

    [Fact]
    public async Task ReplaceStageQualificationRulesAsync_saves_once_under_stage_competition_lockAsync()
    {
        var scenario = CreateQualificationPair();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupMultiStageWriteEnvelope(
            stages,
            matches,
            competitions,
            unitOfWork,
            scenario.Competition,
            scenario.League,
            [scenario.League, scenario.Terminal]);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var path = new QualificationPathSpec(
            1,
            SelectionMode.Position,
            1,
            scenario.Terminal.Id.Value,
            DestinationSlotKey: "Champ");
        await executor.ReplaceStageQualificationRulesAsync(scenario.League.Id, [path]);

        scenario.League.Regulation.QualificationRules.Should().NotBeNull();
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceStageQualificationRulesAsync_when_stage_missing_throws_without_lock_or_saveAsync()
    {
        await AssertStageMissingAsync(stageId => executor =>
            executor.ReplaceStageQualificationRulesAsync(stageId, paths: null));
    }

    [Fact]
    public async Task ReplaceStageQualificationIntentsAsync_saves_once_under_stage_competition_lockAsync()
    {
        var scenario = CreateQualificationPair();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupMultiStageWriteEnvelope(
            stages,
            matches,
            competitions,
            unitOfWork,
            scenario.Competition,
            scenario.League,
            [scenario.League, scenario.Terminal]);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        var intent = new QualificationIntentSpec(
            Guid.NewGuid(),
            1,
            QualificationIntentSourceKind.Overall,
            1,
            1,
            scenario.Terminal.Id.Value,
            DestinationSlotKeys: ["Champ"]);
        await executor.ReplaceStageQualificationIntentsAsync(scenario.League.Id, [intent]);

        scenario.League.Regulation.QualificationRules.Should().NotBeNull();
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceStageQualificationIntentsAsync_when_stage_missing_throws_without_lock_or_saveAsync()
    {
        await AssertStageMissingAsync(stageId => executor =>
            executor.ReplaceStageQualificationIntentsAsync(stageId, intents: null));
    }

    [Fact]
    public async Task ReplaceStagePlacementAwardRulesAsync_saves_once_under_stage_competition_lockAsync()
    {
        var scenario = CreateDraftChampionship();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupStageWriteEnvelope(stages, matches, competitions, unitOfWork, scenario.Competition, scenario.Stage);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        await executor.ReplaceStagePlacementAwardRulesAsync(scenario.Stage.Id, paths: null);

        scenario.Stage.Regulation.PlacementAwardRules.Should().BeNull();
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceStagePlacementAwardRulesAsync_when_stage_missing_throws_without_lock_or_saveAsync()
    {
        await AssertStageMissingAsync(stageId => executor =>
            executor.ReplaceStagePlacementAwardRulesAsync(stageId, paths: null));
    }

    [Fact]
    public async Task ReplaceStageStandingRulesAsync_saves_once_under_stage_competition_lockAsync()
    {
        var scenario = CreateDraftChampionship();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupStageWriteEnvelope(stages, matches, competitions, unitOfWork, scenario.Competition, scenario.Stage);
        var replacement = new StandingRules(new PointsPolicy(2, 1, 0), [RankingCriterion.Points]);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        await executor.ReplaceStageStandingRulesAsync(scenario.Stage.Id, replacement);

        scenario.Stage.Regulation.StandingRules.Should().Be(replacement);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceStageStandingRulesAsync_when_stage_missing_throws_without_lock_or_saveAsync()
    {
        await AssertStageMissingAsync(stageId => executor =>
            executor.ReplaceStageStandingRulesAsync(
                stageId,
                new StandingRules(new PointsPolicy(3, 1, 0), [RankingCriterion.Points])));
    }

    [Fact]
    public async Task ReplaceStageMatchRulesAsync_saves_once_under_stage_competition_lockAsync()
    {
        var scenario = CreateDraftChampionship();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupStageWriteEnvelope(stages, matches, competitions, unitOfWork, scenario.Competition, scenario.Stage);
        var matchRules = scenario.Competition.Regulation.MatchRules!;

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        await executor.ReplaceStageMatchRulesAsync(scenario.Stage.Id, matchRules);

        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceStageMatchRulesAsync_when_stage_missing_throws_without_lock_or_saveAsync()
    {
        await AssertStageMissingAsync(stageId => executor =>
        {
            var rules = SampleRegulations.Standard().MatchRules!;
            return executor.ReplaceStageMatchRulesAsync(stageId, rules);
        });
    }

    [Fact]
    public async Task BindStageRegulationAsync_saves_once_under_stage_competition_lockAsync()
    {
        var scenario = CreateDraftChampionship();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupStageWriteEnvelope(stages, matches, competitions, unitOfWork, scenario.Competition, scenario.Stage);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        await executor.BindStageRegulationAsync(scenario.Stage.Id, BindStageRegulation.ScopeMatch);

        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BindStageRegulationAsync_when_stage_missing_throws_without_lock_or_saveAsync()
    {
        await AssertStageMissingAsync(stageId => executor =>
            executor.BindStageRegulationAsync(stageId, BindStageRegulation.ScopeMatch));
    }

    [Fact]
    public async Task ReplaceStageDrawRulesAsync_saves_once_under_stage_competition_lockAsync()
    {
        var scenario = CreateDraftCupStage();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupStageWriteEnvelope(stages, matches, competitions, unitOfWork, scenario.Competition, scenario.Stage);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        await executor.ReplaceStageDrawRulesAsync(
            scenario.Stage.Id,
            new DrawRules(DrawMode.Random, new SeedingRules(1), new PotRules(2)));

        scenario.Stage.Regulation.DrawRules.Should().NotBeNull();
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceStageDrawRulesAsync_when_stage_missing_throws_without_lock_or_saveAsync()
    {
        await AssertStageMissingAsync(stageId => executor =>
            executor.ReplaceStageDrawRulesAsync(stageId, drawRules: null));
    }

    [Fact]
    public async Task ReplaceStageDefaultTieFormatAsync_saves_once_under_stage_competition_lockAsync()
    {
        var scenario = CreateDraftCupStage();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupStageWriteEnvelope(stages, matches, competitions, unitOfWork, scenario.Competition, scenario.Stage);
        var tieFormat = new TieFormat(TieFormat.TwoLegs, aggregateScoring: true);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        await executor.ReplaceStageDefaultTieFormatAsync(scenario.Stage.Id, tieFormat);

        scenario.Stage.Regulation.TieFormat.Should().Be(tieFormat);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceStageDefaultTieFormatAsync_when_stage_missing_throws_without_lock_or_saveAsync()
    {
        await AssertStageMissingAsync(stageId => executor =>
            executor.ReplaceStageDefaultTieFormatAsync(stageId, tieFormat: null));
    }

    [Fact]
    public async Task ReplaceRoundTieFormatAsync_saves_once_under_stage_competition_lockAsync()
    {
        var scenario = CreateDraftCupStage();
        var roundId = scenario.Stage.Rounds[0].Id;
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupStageWriteEnvelope(stages, matches, competitions, unitOfWork, scenario.Competition, scenario.Stage);
        var tieFormat = new TieFormat(TieFormat.SingleLeg, aggregateScoring: false);

        var executor = CreateExecutor(stages, matches, competitions, unitOfWork);
        await executor.ReplaceRoundTieFormatAsync(scenario.Stage.Id, roundId, tieFormat);

        scenario.Stage.Rounds[0].TieFormat.Should().Be(tieFormat);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceRoundTieFormatAsync_when_stage_missing_throws_without_lock_or_saveAsync()
    {
        await AssertStageMissingAsync(stageId => executor =>
            executor.ReplaceRoundTieFormatAsync(
                stageId,
                RoundId.New(),
                new TieFormat(TieFormat.SingleLeg, aggregateScoring: false)));
    }

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

    private static void SetupStageWriteEnvelope(
        Mock<IStageRepository> stages,
        Mock<IMatchRepository> matches,
        Mock<ICompetitionRepository> competitions,
        Mock<IUnitOfWork> unitOfWork,
        Competition competition,
        Stage stage)
    {
        SetupExclusive(unitOfWork);
        SetupCompetitionId(stages, stage.Id, competition.Id);
        stages
            .Setup(repository => repository.GetByIdForUpdateAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        stages
            .Setup(repository => repository.GetByIdsForUpdateAsync(competition.StageIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync([stage]);
        matches
            .Setup(repository => repository.ListByStageForUpdateAsync(It.IsAny<StageId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        matches
            .Setup(repository => repository.ListByStageIdsForUpdateAsync(
                It.IsAny<IReadOnlyList<StageId>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<StageId, IReadOnlyList<global::MyClub.PlayUp.Domain.Matches.Match>>());
        SetupStructureReads(matches, stages, competition.Id, [stage]);
    }

    private static void SetupMultiStageWriteEnvelope(
        Mock<IStageRepository> stages,
        Mock<IMatchRepository> matches,
        Mock<ICompetitionRepository> competitions,
        Mock<IUnitOfWork> unitOfWork,
        Competition competition,
        Stage primary,
        IReadOnlyList<Stage> loadedStages)
    {
        SetupExclusive(unitOfWork);
        SetupCompetitionId(stages, primary.Id, competition.Id);
        stages
            .Setup(repository => repository.GetByIdForUpdateAsync(primary.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(primary);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        stages
            .Setup(repository => repository.GetByIdsForUpdateAsync(competition.StageIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<StageId> ids, CancellationToken _) =>
                loadedStages.Where(candidate => ids.Contains(candidate.Id)).ToList());
        matches
            .Setup(repository => repository.ListByStageForUpdateAsync(It.IsAny<StageId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        matches
            .Setup(repository => repository.ListByStageIdsForUpdateAsync(
                It.IsAny<IReadOnlyList<StageId>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<StageId, IReadOnlyList<global::MyClub.PlayUp.Domain.Matches.Match>>());
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

    private static async Task AssertStageMissingAsync(Func<StageId, Func<UseCaseExecutor, Task>> invoke)
    {
        var stageId = StageId.New();
        var (stages, matches, competitions, unitOfWork) = CreateRepositories();
        SetupCompetitionId(stages, stageId, null);

        var executor = new UseCaseExecutor(
            stages.Object,
            matches.Object,
            competitions.Object,
            unitOfWork.Object,
            new FakeClock(new DateTimeOffset(2026, 11, 23, 19, 0, 0, TimeSpan.Zero)),
            AlwaysExistingMedia.Instance,
            NullLogger<UseCaseExecutor>.Instance);
        var act = () => invoke(stageId)(executor);

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

    private static (
        Mock<IStageRepository> Stages,
        Mock<IMatchRepository> Matches,
        Mock<ICompetitionRepository> Competitions,
        Mock<IUnitOfWork> UnitOfWork) CreateRepositories()
    {
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
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
        var competition = CreateCompetition.Execute("Cup replace", _clock);
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(4), _clock).Stage;
        return new StageScenario(competition, stage);
    }

    private StageScenario CreateDraftChampionship()
    {
        var competition = CreateCompetition.Execute("Champ replace", _clock);
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Championship("Main"), _clock).Stage;
        return new StageScenario(competition, stage);
    }

    private QualificationScenario CreateQualificationPair()
    {
        var competition = CreateCompetition.Execute("Qualif replace", _clock);
        var league = AddCompetitionStage.Execute(competition, StructureIntent.Championship("League"), _clock);
        var terminal = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Terminal"), _clock);
        terminal.AddSlot("Champ");
        return new QualificationScenario(competition, league, terminal);
    }

    private sealed record StageScenario(Competition Competition, Stage Stage);

    private sealed record QualificationScenario(Competition Competition, Stage League, Stage Terminal);
}
