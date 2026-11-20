// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorCupSlotLockTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;
using DomainMatch = MyClub.PlayUp.Domain.Matches.Match;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

/// <summary>
/// Competition lock around slot placement and match production writes.
/// </summary>
public sealed class UseCaseExecutorCupSlotLockTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task AssignEntryToSlotAsync_locks_loads_inside_and_saves_onceAsync()
    {
        var (competition, stage, entryId) = SeedCupPopulation();
        var harness = new Harness(competition, stage);
        harness.ArmExclusive();

        await harness.Executor.AssignEntryToSlotAsync(stage.Id, "S1", entryId, expectedEntryId: null);

        harness.EnteredLock.Should().BeTrue();
        stage.FindSlot("S1")!.EntryId.Should().Be(entryId);
        harness.UnitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        harness.UnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignEntryToSlotAsync_noop_does_not_saveAsync()
    {
        var (competition, stage, entryId) = SeedCupPopulation();
        stage.AssignEntryToSlot("S1", entryId);
        var harness = new Harness(competition, stage);
        harness.ArmExclusive();

        await harness.Executor.AssignEntryToSlotAsync(stage.Id, "S1", entryId, entryId);

        harness.UnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClearSlotAssignmentAsync_locks_loads_inside_and_saves_onceAsync()
    {
        var (competition, stage, entryId) = SeedCupPopulation();
        stage.AssignEntryToSlot("S1", entryId);
        var harness = new Harness(competition, stage);
        harness.ArmExclusive();

        await harness.Executor.ClearSlotAssignmentAsync(stage.Id, "S1", entryId);

        harness.EnteredLock.Should().BeTrue();
        stage.FindSlot("S1")!.EntryId.Should().BeNull();
        harness.UnitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        harness.UnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MaterializeMatchesAsync_locks_loads_inside_and_saves_onceAsync()
    {
        var (competition, stage) = SeedReadyGroups();
        var harness = new Harness(competition, stage);
        harness.ArmExclusive();
        harness.Matches
            .Setup(repository => repository.Add(It.IsAny<DomainMatch>()));

        var result = await harness.Executor.MaterializeMatchesAsync(stage.Id);

        harness.EnteredLock.Should().BeTrue();
        result.CreatedMatches.Should().NotBeEmpty();
        result.AlreadyComplete.Should().BeFalse();
        harness.UnitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        harness.UnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        harness.Matches.Verify(repository => repository.Add(It.IsAny<DomainMatch>()), Times.Exactly(result.CreatedMatches.Count));
    }

    [Fact]
    public async Task MaterializeCupFromOccupiedSlotsAsync_locks_loads_inside_and_saves_onceAsync()
    {
        var (competition, stage) = SeedReadyCup();
        var harness = new Harness(competition, stage);
        harness.ArmExclusive();
        harness.Matches
            .Setup(repository => repository.Add(It.IsAny<DomainMatch>()));
        harness.Matches
            .Setup(repository => repository.ListSummaryRowsByStageIdsReadOnlyAsync(
                It.IsAny<IReadOnlyList<StageId>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>());

        var result = await harness.Executor.MaterializeCupFromOccupiedSlotsAsync(
            stage.Id,
            pairKeys: null,
            planEntireRound: true);

        harness.EnteredLock.Should().BeTrue();
        result.CreatedMatches.Should().NotBeEmpty();
        harness.UnitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        harness.UnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DematerializePhaseAsync_locks_loads_inside_and_saves_onceAsync()
    {
        var (competition, stage) = SeedReadyGroups();
        var created = MaterializeMatches.Execute(competition, stage, [], _clock, [stage]).CreatedMatches;
        var harness = new Harness(competition, stage, created);
        harness.ArmExclusive();
        harness.Matches
            .Setup(repository => repository.Remove(It.IsAny<DomainMatch>()));

        var result = await harness.Executor.DematerializePhaseAsync(stage.Id);

        harness.EnteredLock.Should().BeTrue();
        result.RemovedMatchCount.Should().Be(created.Count);
        harness.UnitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        harness.UnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        harness.Matches.Verify(repository => repository.Remove(It.IsAny<DomainMatch>()), Times.Exactly(created.Count));
    }

    [Fact]
    public async Task Locked_writes_report_StageNotFound_without_taking_the_lockAsync()
    {
        var stageId = StageId.New();
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        stages
            .Setup(repository => repository.GetCompetitionIdReadOnlyAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompetitionId?)null);
        var executor = new UseCaseExecutor(
            stages.Object,
            matches.Object,
            competitions.Object,
            unitOfWork.Object,
            _clock,
            AlwaysExistingMedia.Instance,
            NullLogger<UseCaseExecutor>.Instance);

        var actions = new Func<Task>[]
        {
            () => executor.AssignEntryToSlotAsync(stageId, "S1", EntryId.New(), null),
            () => executor.ClearSlotAssignmentAsync(stageId, "S1", null),
            () => executor.MaterializeMatchesAsync(stageId),
            () => executor.MaterializeCupFromOccupiedSlotsAsync(stageId, null),
            () => executor.DematerializePhaseAsync(stageId),
        };

        foreach (var action in actions)
        {
            var exception = await action.Should().ThrowAsync<ApplicationFailureException>();
            exception.Which.Code.Should().Be(ApplicationErrorCodes.StageNotFound);
        }

        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                It.IsAny<CompetitionId>(),
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Assign_and_clear_report_CompetitionClosedAsync()
    {
        var (competition, stage, entryId) = SeedCupPopulation();
        Close(competition);
        var harness = new Harness(competition, stage);
        harness.ArmExclusive();

        var assign = async () => await harness.Executor.AssignEntryToSlotAsync(stage.Id, "S1", entryId, null);
        var clear = async () => await harness.Executor.ClearSlotAssignmentAsync(stage.Id, "S1", null);

        (await assign.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.CompetitionClosed);
        (await clear.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.CompetitionClosed);
        stage.FindSlot("S1")!.EntryId.Should().BeNull();
        harness.UnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Production_writes_keep_their_existing_closed_competition_refusalAsync()
    {
        var (groupsCompetition, groups) = SeedReadyGroups();
        Close(groupsCompetition);
        var groupsHarness = new Harness(groupsCompetition, groups);
        groupsHarness.ArmExclusive();

        var (cupCompetition, cup) = SeedReadyCup();
        Close(cupCompetition);
        var cupHarness = new Harness(cupCompetition, cup);
        cupHarness.ArmExclusive();
        cupHarness.Matches
            .Setup(repository => repository.ListSummaryRowsByStageIdsReadOnlyAsync(
                It.IsAny<IReadOnlyList<StageId>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>());

        var materialize = async () => await groupsHarness.Executor.MaterializeMatchesAsync(groups.Id);
        var dematerialize = async () => await groupsHarness.Executor.DematerializePhaseAsync(groups.Id);
        var cupMaterialize = async () =>
            await cupHarness.Executor.MaterializeCupFromOccupiedSlotsAsync(cup.Id, null, planEntireRound: true);

        (await materialize.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().NotBe(ApplicationErrorCodes.CompetitionClosed);
        (await dematerialize.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().NotBe(ApplicationErrorCodes.CompetitionClosed);
        (await cupMaterialize.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().NotBe(ApplicationErrorCodes.CompetitionClosed);
        groupsHarness.UnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        cupHarness.UnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private (Competition Competition, Stage Stage, EntryId EntryId) SeedCupPopulation()
    {
        var competition = CreateCompetition.Execute("Lock-Cup", _clock);
        var stage = AddCompetitionStage.Execute(competition, StructureIntent.Cup(2, "KO"), _clock);
        var entry = competition.AddEntry(TeamId.New(), "Team A", _clock);
        ReplaceStageAffectationAuthoring.Execute(stage, competition, [entry.Id], _clock);
        return (competition, stage, entry.Id);
    }

    private (Competition Competition, Stage Stage) SeedReadyGroups()
    {
        var competition = CreateCompetition.Execute("Lock-Groups", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 2), _clock).Stage;
        stage.ReplaceAffectationAuthoring(ids, _clock);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[1]);
        stage.AssignEntryToGroup(stage.Groups[1].Id, ids[2]);
        stage.AssignEntryToGroup(stage.Groups[1].Id, ids[3]);
        stage.Prepare(_clock);
        return (competition, stage);
    }

    private (Competition Competition, Stage Stage) SeedReadyCup()
    {
        var competition = CreateCompetition.Execute("Lock-Cup-Ready", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"C{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(4), _clock).Stage;
        stage.ReplaceAffectationAuthoring(ids, _clock);
        var assigned = 0;
        foreach (var slot in stage.Slots)
        {
            if (assigned >= ids.Length)
            {
                break;
            }

            try
            {
                stage.AssignEntryToSlot(slot.SlotKey, ids[assigned]);
                assigned++;
            }
            catch (DomainException exception) when (exception.Code == StageErrorCodes.SlotFeedConflict)
            {
                continue;
            }
        }

        assigned.Should().Be(ids.Length);
        stage.Prepare(_clock);
        return (competition, stage);
    }

    private void Close(Competition competition)
    {
        competition.Prepare(_clock);
        competition.Start(_clock);
        competition.Complete(CompletionMode.Administrative, _clock);
    }

    private sealed class Harness
    {
        private readonly bool[] _inside = [false];

        public Harness(Competition competition, Stage stage, IReadOnlyList<DomainMatch>? existingMatches = null)
        {
            Competition = competition;
            Stage = stage;
            Stages = new Mock<IStageRepository>(MockBehavior.Strict);
            Matches = new Mock<IMatchRepository>(MockBehavior.Strict);
            Competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
            UnitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

            Stages
                .Setup(repository => repository.GetCompetitionIdReadOnlyAsync(stage.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(competition.Id);
            Stages
                .Setup(repository => repository.GetByIdForUpdateAsync(stage.Id, It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    _inside[0].Should().BeTrue();
                    return Task.FromResult<Stage?>(stage);
                });
            Stages
                .Setup(repository => repository.GetByIdsForUpdateAsync(
                    It.IsAny<IReadOnlyList<StageId>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    _inside[0].Should().BeTrue();
                    return Task.FromResult<IReadOnlyList<Stage>>([stage]);
                });
            Competitions
                .Setup(repository => repository.GetByIdForUpdateAsync(competition.Id, It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    _inside[0].Should().BeTrue();
                    return Task.FromResult<Competition?>(competition);
                });
            Matches
                .Setup(repository => repository.ListByStageForUpdateAsync(stage.Id, It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    _inside[0].Should().BeTrue();
                    return Task.FromResult<IReadOnlyList<DomainMatch>>(existingMatches ?? []);
                });
            UnitOfWork
                .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Executor = new UseCaseExecutor(
                Stages.Object,
                Matches.Object,
                Competitions.Object,
                UnitOfWork.Object,
                new FakeClock(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero)),
                AlwaysExistingMedia.Instance,
                NullLogger<UseCaseExecutor>.Instance);
        }

        public Competition Competition { get; }

        public Stage Stage { get; }

        public Mock<IStageRepository> Stages { get; }

        public Mock<IMatchRepository> Matches { get; }

        public Mock<ICompetitionRepository> Competitions { get; }

        public Mock<IUnitOfWork> UnitOfWork { get; }

        public UseCaseExecutor Executor { get; }

        public bool EnteredLock { get; private set; }

        public void ArmExclusive() =>
            UnitOfWork
                .Setup(uow => uow.ExecuteExclusiveAsync(
                    Competition.Id,
                    It.IsAny<Func<CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((CompetitionId _, Func<CancellationToken, Task> write, CancellationToken token) =>
                {
                    EnteredLock = true;
                    _inside[0] = true;
                    return write(token);
                });
    }
}
