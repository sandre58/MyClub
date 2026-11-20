// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorGroupDrawLockTests.cs" company="Stéphane ANDRE">
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
/// Create, configure, and generate take the competition lock and load inside it.
/// </summary>
public sealed class UseCaseExecutorGroupDrawLockTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Create_configure_and_generate_lock_load_inside_and_save_onceAsync()
    {
        var (competition, stage) = OpenGroups();
        var harness = new Harness(competition, stage);
        harness.ArmExclusive();

        var created = await harness.Executor.CreateDrawAsync(stage.Id, DrawResolutionKind.Group);

        created.Status.Should().Be(DrawStatus.Draft);
        harness.EnteredLock.Should().BeTrue();
        stage.Draws.Should().ContainSingle();
        harness.UnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        var configured = await harness.Executor.ConfigureDrawInputsAsync(stage.Id, new DrawId(created.DrawId));

        configured.Status.Should().Be(DrawStatus.Draft);
        stage.GetDraw(new DrawId(created.DrawId)).Inputs.Should().NotBeNull();
        harness.UnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));

        var generated = await harness.Executor.GenerateDrawAsync(stage.Id, new DrawId(created.DrawId));

        generated.IsResolved.Should().BeTrue();
        stage.GetDraw(new DrawId(created.DrawId)).Resolution.State.Should().Be(DrawResolutionState.Resolved);
        harness.UnitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        harness.UnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Missing_stage_is_reported_without_taking_the_lockAsync()
    {
        var stageId = StageId.New();
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        stages
            .Setup(repository => repository.GetCompetitionIdReadOnlyAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompetitionId?)null);
        var executor = new UseCaseExecutor(
            stages.Object,
            new Mock<IMatchRepository>(MockBehavior.Strict).Object,
            new Mock<ICompetitionRepository>(MockBehavior.Strict).Object,
            unitOfWork.Object,
            _clock,
            AlwaysExistingMedia.Instance,
            NullLogger<UseCaseExecutor>.Instance);

        var actions = new Func<Task>[]
        {
            () => executor.CreateDrawAsync(stageId, DrawResolutionKind.Group),
            () => executor.ConfigureDrawInputsAsync(stageId, DrawId.New()),
            () => executor.GenerateDrawAsync(stageId, DrawId.New()),
        };

        foreach (var action in actions)
        {
            (await action.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
                .Should().Be(ApplicationErrorCodes.StageNotFound);
        }

        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                It.IsAny<CompetitionId>(),
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Closed_competition_is_refused_inside_the_lock_without_savingAsync()
    {
        var (competition, stage) = OpenGroups();
        competition.Prepare(_clock);
        competition.Start(_clock);
        competition.Complete(CompletionMode.Administrative, _clock);
        var harness = new Harness(competition, stage);
        harness.ArmExclusive();

        var act = async () => await harness.Executor.CreateDrawAsync(stage.Id, DrawResolutionKind.Group);

        (await act.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.CompetitionClosed);
        harness.EnteredLock.Should().BeTrue();
        stage.Draws.Should().BeEmpty();
        harness.UnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private (Competition Competition, Stage Stage) OpenGroups()
    {
        var competition = CreateCompetition.Execute("Lock", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 2), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random, potRules: new PotRules(2)), _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        return (competition, stage);
    }

    private sealed class Harness
    {
        private readonly bool[] _inside = [false];

        public Harness(Competition competition, Stage stage)
        {
            Competition = competition;
            Stage = stage;
            Stages = new Mock<IStageRepository>(MockBehavior.Strict);
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
            UnitOfWork
                .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Executor = new UseCaseExecutor(
                Stages.Object,
                new Mock<IMatchRepository>(MockBehavior.Strict).Object,
                Competitions.Object,
                UnitOfWork.Object,
                new FakeClock(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero)),
                AlwaysExistingMedia.Instance,
                NullLogger<UseCaseExecutor>.Instance);
        }

        public Competition Competition { get; }

        public Stage Stage { get; }

        public Mock<IStageRepository> Stages { get; }

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
