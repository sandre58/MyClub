// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorMatchDisappearedDuringLockTests.cs" company="Stéphane ANDRE">
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
using Xunit;
using DomainMatch = MyClub.PlayUp.Domain.Matches.Match;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

/// <summary>
/// Match row removed while another writer was waiting on the competition lock (B1 application gate).
/// </summary>
public sealed class UseCaseExecutorMatchDisappearedDuringLockTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 13, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task StartMatchAsync_when_match_gone_inside_lock_after_projection_returns_MatchNotFoundAsync()
    {
        var competitionId = CompetitionId.New();
        var match = DomainMatch.Create(
            competitionId,
            StageId.New(),
            EntryId.New(),
            EntryId.New(),
            _clock);
        var matchId = match.Id;

        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var inside = false;

        matchRepo
            .Setup(repository => repository.GetCompetitionIdReadOnlyAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competitionId);
        matchRepo
            .Setup(repository => repository.GetByIdForUpdateAsync(matchId, It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                inside.Should().BeTrue("match load must happen under the competition lock");
                return Task.FromResult<DomainMatch?>(null);
            });
        unitOfWork
            .Setup(uow => uow.ExecuteExclusiveAsync(
                competitionId,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (CompetitionId _, Func<CancellationToken, Task> write, CancellationToken token) =>
            {
                inside = true;
                await write(token);
            });

        var executor = new UseCaseExecutor(
            stages.Object,
            matchRepo.Object,
            competitions.Object,
            unitOfWork.Object,
            _clock,
            AlwaysExistingMedia.Instance,
            NullLogger<UseCaseExecutor>.Instance);

        var act = async () => await executor.StartMatchAsync(matchId);

        (await act.Should().ThrowAsync<ApplicationFailureException>())
            .Which.Code.Should().Be(ApplicationErrorCodes.MatchNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
