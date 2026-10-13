// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorReadTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;
using DomainMatch = MyClub.PlayUp.Domain.Matches.Match;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

public sealed class UseCaseExecutorReadTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 22, 30, 0, TimeSpan.Zero));

    [Fact]
    public async Task GetCompetitionDetailAsync_when_missing_throws_and_does_not_saveAsync()
    {
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        competitions
            .Setup(repository => repository.GetByIdReadOnlyAsync(It.IsAny<CompetitionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Competition?)null);

        var executor = new UseCaseExecutor(stages.Object, matches.Object, competitions.Object, unitOfWork.Object, _clock, AlwaysExistingMedia.Instance, NullLogger<UseCaseExecutor>.Instance);
        var act = async () => await executor.GetCompetitionDetailAsync(CompetitionId.New());

        (await act.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.CompetitionNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStageOverviewAsync_when_missing_throws_and_does_not_saveAsync()
    {
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        stages
            .Setup(repository => repository.GetByIdReadOnlyAsync(
                It.IsAny<StageId>(),
                StageLoadProfile.Full,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stage?)null);

        var executor = new UseCaseExecutor(stages.Object, matches.Object, competitions.Object, unitOfWork.Object, _clock, AlwaysExistingMedia.Instance, NullLogger<UseCaseExecutor>.Instance);
        var act = async () => await executor.GetStageOverviewAsync(StageId.New());

        (await act.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.StageNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetMatchDetailAsync_when_missing_throws_and_does_not_saveAsync()
    {
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        matches
            .Setup(repository => repository.GetByIdReadOnlyAsync(It.IsAny<MatchId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainMatch?)null);

        var executor = new UseCaseExecutor(stages.Object, matches.Object, competitions.Object, unitOfWork.Object, _clock, AlwaysExistingMedia.Instance, NullLogger<UseCaseExecutor>.Instance);
        var act = async () => await executor.GetMatchDetailAsync(MatchId.New());

        (await act.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.MatchNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListMatchesByStageAsync_loads_and_assembles_without_saveAsync()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        competition.AddStage(stage.Id, _clock);
        var match = DomainMatch.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        var row = new MatchSummaryRow(match.Id, match.StageId, match.Status, match.HomeEntryId, match.AwayEntryId, match.Result);

        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages.Setup(repository => repository.GetByIdReadOnlyAsync(
            stage.Id,
            StageLoadProfile.Structure,
            It.IsAny<CancellationToken>())).ReturnsAsync(stage);
        competitions
            .Setup(repository => repository.GetByIdReadOnlyAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        matches
            .Setup(repository => repository.ListSummaryRowsByStageReadOnlyAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([row]);

        var executor = new UseCaseExecutor(stages.Object, matches.Object, competitions.Object, unitOfWork.Object, _clock, AlwaysExistingMedia.Instance, NullLogger<UseCaseExecutor>.Instance);
        var summaries = await executor.ListMatchesByStageAsync(stage.Id);

        summaries.Should().ContainSingle();
        summaries[0].Home.DisplayName.Should().Be("Home");
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStageSchematicAsync_loads_target_with_draws_and_peers_with_structure_onlyAsync()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var source = Stage.Create(competition.Id, new StageName("R32"), SampleRegulations.Standard(), _clock);
        source.AddRound("R32", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        source.AddSlot("S1");
        var target = Stage.Create(competition.Id, new StageName("R16"), SampleRegulations.Standard(), _clock);
        target.AddRound("R16", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        target.AddSlot("R16-A");
        competition.AddStage(source.Id, _clock);
        competition.AddStage(target.Id, _clock);

        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        stages
            .Setup(repository => repository.GetByIdsReadOnlyAsync(
                It.Is<IReadOnlyList<StageId>>(ids => ids.Single().Equals(target.Id)),
                It.Is<StageReadCapabilities>(capabilities =>
                    capabilities.Profile == StageLoadProfile.Structure
                    && capabilities.IncludeDraws
                    && capabilities.IncludeSlots),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([target]);
        stages
            .Setup(repository => repository.GetByIdsReadOnlyAsync(
                It.Is<IReadOnlyList<StageId>>(ids => ids.Single().Equals(source.Id)),
                It.Is<StageReadCapabilities>(capabilities =>
                    capabilities.Profile == StageLoadProfile.Structure
                    && !capabilities.IncludeDraws
                    && !capabilities.IncludeSlots),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([source]);
        competitions
            .Setup(repository => repository.GetByIdReadOnlyAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        matches
            .Setup(repository => repository.ListSummaryRowsByStageReadOnlyAsync(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var executor = new UseCaseExecutor(
            stages.Object,
            matches.Object,
            competitions.Object,
            unitOfWork.Object,
            _clock,
            AlwaysExistingMedia.Instance,
            NullLogger<UseCaseExecutor>.Instance);

        var schematic = await executor.GetStageSchematicAsync(target.Id);

        schematic.StageId.Should().Be(target.Id.Value);
        schematic.Cases.Should().ContainSingle(c => c.FormPosition.SlotKey == "R16-A");
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
