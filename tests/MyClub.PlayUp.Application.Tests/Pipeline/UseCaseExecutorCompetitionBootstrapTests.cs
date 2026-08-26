// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorCompetitionBootstrapTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Moq;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

public sealed class UseCaseExecutorCompetitionBootstrapTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 8, 15, 0, TimeSpan.Zero));

    [Fact]
    public async Task CreateCompetitionAsync_adds_saves_and_returns_workspace_summaryAsync()
    {
        Competition? added = null;
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        competitions
            .Setup(repository => repository.Add(It.IsAny<Competition>()))
            .Callback<Competition>(competition => added = competition);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = new UseCaseExecutor(stages.Object, matches.Object, competitions.Object, unitOfWork.Object, _clock, AlwaysExistingMedia.Instance);
        var summary = await executor.CreateCompetitionAsync("Bootstrap Cup");

        added.Should().NotBeNull();
        summary.Id.Should().Be(added!.Id.Value);
        summary.Name.Should().Be("Bootstrap Cup");
        summary.Status.Should().Be(CompetitionStatus.Draft);
        summary.NextActionCode.Should().Be(WorkspaceSummaryAssembler.ContinueOrganisationCode);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListCompetitionsAsync_returns_assembled_rowsAsync()
    {
        var first = CreateCompetition.Execute("Alpha", _clock);
        var second = CreateCompetition.Execute("Beta", _clock);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        competitions
            .Setup(repository => repository.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([first, second]);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var executor = new UseCaseExecutor(stages.Object, matches.Object, competitions.Object, unitOfWork.Object, _clock, AlwaysExistingMedia.Instance);
        var list = await executor.ListCompetitionsAsync();

        list.Should().HaveCount(2);
        list.Select(item => item.Name).Should().Equal("Alpha", "Beta");
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetWorkspaceSummaryAsync_when_missing_throws_and_does_not_saveAsync()
    {
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        competitions
            .Setup(repository => repository.GetByIdAsync(It.IsAny<CompetitionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Competition?)null);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var executor = new UseCaseExecutor(stages.Object, matches.Object, competitions.Object, unitOfWork.Object, _clock, AlwaysExistingMedia.Instance);
        var act = async () => await executor.GetWorkspaceSummaryAsync(CompetitionId.New());

        (await act.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.CompetitionNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
