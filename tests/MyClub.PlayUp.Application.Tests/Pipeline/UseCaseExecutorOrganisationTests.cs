// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorOrganisationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Moq;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

public sealed class UseCaseExecutorOrganisationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 10, 30, 0, TimeSpan.Zero));

    [Fact]
    public async Task ConfigureStructureAsync_creates_stage_adds_and_saves_onceAsync()
    {
        var competition = CreateCompetition.Execute("Exec Org", _clock);
        Stage? addedStage = null;
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        competitions
            .Setup(repository => repository.GetByIdAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        stages
            .Setup(repository => repository.Add(It.IsAny<Stage>()))
            .Callback<Stage>(stage => addedStage = stage);
        stages
            .Setup(repository => repository.GetByIdAsync(It.IsAny<StageId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StageId id, CancellationToken _) =>
                addedStage?.Id.Equals(id) == true ? addedStage : null);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = new UseCaseExecutor(stages.Object, matches.Object, competitions.Object, unitOfWork.Object, _clock);
        var view = await executor.ConfigureStructureAsync(
            competition.Id,
            StructureIntent.Groups(2, 4));

        addedStage.Should().NotBeNull();
        view.Structure.GroupCount.Should().Be(2);
        view.Format.Kind.Should().Be(StructureFormatKind.Groups);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        stages.Verify(repository => repository.Add(It.IsAny<Stage>()), Times.Once);
    }

    [Fact]
    public async Task AddEntryAsync_persists_and_returns_organisation_viewAsync()
    {
        var competition = CreateCompetition.Execute("Entries", _clock);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        competitions
            .Setup(repository => repository.GetByIdAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = new UseCaseExecutor(stages.Object, matches.Object, competitions.Object, unitOfWork.Object, _clock);
        var view = await executor.AddEntryAsync(competition.Id, "Team One");

        view.Participants.ActiveCount.Should().Be(1);
        view.Participants.Entries.Should().ContainSingle(entry => entry.DisplayName == "Team One");
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
