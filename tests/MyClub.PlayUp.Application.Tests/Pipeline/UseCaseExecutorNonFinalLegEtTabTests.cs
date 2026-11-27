// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorNonFinalLegEtTabTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;
using DomainMatch = MyClub.PlayUp.Domain.Matches.Match;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

public sealed class UseCaseExecutorNonFinalLegEtTabTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 27, 20, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task FinishMatchAsync_rejects_extra_time_on_first_leg_without_saveAsync()
    {
        var (competition, stage, first, _) = CreateTwoLegCup();
        first.Start(_clock);
        var result = new MatchResult(ResultType.Played, new Score(1, 1), extraTimePlayed: true);
        var executor = CreateExecutor(competition, stage, first);

        var act = async () => await executor.FinishMatchAsync(first.Id, result);

        var exception = await act.Should().ThrowAsync<DomainException>();
        exception.Which.Code.Should().Be(MatchErrorCodes.ExtraTimeOrShootoutOnNonFinalLeg);
        first.Status.Should().Be(MatchStatus.Live);
    }

    [Fact]
    public async Task FinishMatchAsync_allows_extra_time_on_final_leg_and_savesAsync()
    {
        var (competition, stage, _, second) = CreateTwoLegCup();
        second.Start(_clock);
        var result = new MatchResult(ResultType.Played, new Score(0, 0), extraTimePlayed: true);
        var executor = CreateExecutor(competition, stage, second);

        await executor.FinishMatchAsync(second.Id, result);

        second.Status.Should().Be(MatchStatus.Finished);
        second.Result!.ExtraTimePlayed.Should().BeTrue();
    }

    [Fact]
    public async Task CorrectMatchResultAsync_rejects_shootout_on_first_leg_without_saveAsync()
    {
        var (competition, stage, first, _) = CreateTwoLegCup();
        first.Start(_clock);
        first.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        var illegal = new MatchResult(
            ResultType.Played,
            new Score(1, 1),
            penaltyShootoutScore: new PenaltyShootoutScore(4, 3));
        var executor = CreateExecutor(competition, stage, first);

        var act = async () => await executor.CorrectMatchResultAsync(first.Id, illegal);

        var exception = await act.Should().ThrowAsync<DomainException>();
        exception.Which.Code.Should().Be(MatchErrorCodes.ExtraTimeOrShootoutOnNonFinalLeg);
        first.Result!.PenaltyShootoutScore.Should().BeNull();
    }

    [Fact]
    public async Task CorrectMatchResultAsync_allows_removing_illegal_et_tab_on_first_legAsync()
    {
        var (competition, stage, first, _) = CreateTwoLegCup();
        first.Start(_clock);
        first.Finish(
            new MatchResult(
                ResultType.Played,
                new Score(1, 1),
                extraTimePlayed: true,
                penaltyShootoutScore: new PenaltyShootoutScore(4, 3)),
            _clock);
        var cleaned = new MatchResult(ResultType.Played, new Score(1, 1));
        var executor = CreateExecutor(competition, stage, first);

        await executor.CorrectMatchResultAsync(first.Id, cleaned);

        first.Result.Should().Be(cleaned);
        first.Result!.ExtraTimePlayed.Should().BeFalse();
        first.Result.PenaltyShootoutScore.Should().BeNull();
    }

    private UseCaseExecutor CreateExecutor(Competition competition, Stage stage, DomainMatch match)
    {
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        matchRepo
            .Setup(repository => repository.GetCompetitionIdReadOnlyAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition.Id);
        unitOfWork
            .Setup(uow => uow.ExecuteExclusiveAsync(competition.Id, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<CompetitionId, Func<CancellationToken, Task>, CancellationToken>(
                (_, write, token) => write(token));
        matchRepo
            .Setup(repository => repository.GetByIdForUpdateAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);
        competitions
            .Setup(repository => repository.GetByIdForUpdateAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        stages
            .Setup(repository => repository.GetByIdForUpdateAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new UseCaseExecutor(
            stages.Object,
            matchRepo.Object,
            competitions.Object,
            unitOfWork.Object,
            _clock,
            AlwaysExistingMedia.Instance,
            NullLogger<UseCaseExecutor>.Instance);
    }

    private (Competition Competition, Stage Stage, DomainMatch First, DomainMatch Second) CreateTwoLegCup()
    {
        var competition = Competition.Create(new CompetitionName("Cup ET/TAB"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.TwoLegs, aggregateScoring: true), _clock);
        stage.AddSlot("A");
        stage.AddSlot("B");
        stage.ReplaceBracketPairs([new BracketPair("P1", "A", "B")]);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock).Id;
        var away = competition.AddEntry(TeamId.New(), "Away", _clock).Id;
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, "A", "B", "P1");
        stage.AddResolvedPopulationEntry(home, _clock);
        stage.AddResolvedPopulationEntry(away, _clock);
        stage.AssignEntryToSlot("A", home);
        stage.AssignEntryToSlot("B", away);
        var first = DomainMatch.Create(competition.Id, stage.Id, home, away, _clock);
        var second = DomainMatch.Create(competition.Id, stage.Id, away, home, _clock);
        stage.AttachMatch(fixture.Id, first.Id, legIndex: 1, _clock);
        stage.AttachMatch(fixture.Id, second.Id, legIndex: 2, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        return (competition, stage, first, second);
    }
}
