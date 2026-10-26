// -----------------------------------------------------------------------
// <copyright file="InsertCompetitionStageExecutorTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

/// <summary>
/// Insert fails before the unit of work: no stage is added and SaveChanges is not called.
/// </summary>
public sealed class InsertCompetitionStageExecutorTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 26, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Insert_invalid_anchor_does_not_add_or_saveAsync()
    {
        var competition = CreateCompetition.Execute("Exec-Insert", _clock);
        var only = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Only"), _clock);

        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var executor = new UseCaseExecutor(
            stages.Object,
            matches.Object,
            competitions.Object,
            unitOfWork.Object,
            _clock,
            AlwaysExistingMedia.Instance,
            NullLogger<UseCaseExecutor>.Instance);

        var act = () => executor.InsertCompetitionStageAsync(
            competition.Id,
            StructureIntent.Championship("Ghost"),
            StageId.New(),
            StageInsertPosition.Before);

        var failure = (await act.Should().ThrowAsync<ApplicationFailureException>()).Which;
        failure.Code.Should().Be(ApplicationErrorCodes.StageNotInCompetition);
        competition.StageIds.Should().Equal(only.Id);
        stages.Verify(repository => repository.Add(It.IsAny<Stage>()), Times.Never);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reorder_when_running_does_not_saveAsync()
    {
        var competition = CreateCompetition.Execute("Exec-Order", _clock);
        var first = AddCompetitionStage.Execute(competition, StructureIntent.Cup(2, "First"), _clock);
        var second = AddCompetitionStage.Execute(competition, StructureIntent.Cup(2, "Second"), _clock);
        AddEntry.Execute(competition, "Alpha", _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var executor = new UseCaseExecutor(
            stages.Object,
            matches.Object,
            competitions.Object,
            unitOfWork.Object,
            _clock,
            AlwaysExistingMedia.Instance,
            NullLogger<UseCaseExecutor>.Instance);

        var act = () => executor.SetCompetitionStageOrderAsync(competition.Id, [second.Id, first.Id]);

        var failure = (await act.Should().ThrowAsync<ApplicationFailureException>()).Which;
        failure.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
        competition.StageIds.Should().Equal(first.Id, second.Id);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
