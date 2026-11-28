// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorMatchResultEntryShapeTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Matches;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

public sealed class UseCaseExecutorMatchResultEntryShapeTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 28, 18, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task FinishMatchFromEntryAsync_faults_task_when_result_type_unknownAsync()
    {
        var executor = CreateStrictExecutor();
        var entry = new MatchResultEntry((ResultType)99, 1, 0);
        Task? task = null;
        var synchronousException = Record.Exception(() =>
        {
            task = executor.FinishMatchFromEntryAsync(MatchId.New(), entry);
        });

        synchronousException.Should().BeNull();
        task!.IsFaulted.Should().BeTrue();
        Func<Task> awaitFaulted = async () => await task!.ConfigureAwait(false);
        (await awaitFaulted.Should().ThrowAsync<DomainException>())
            .Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    [Fact]
    public async Task CorrectMatchResultFromEntryAsync_faults_task_when_shootout_partialAsync()
    {
        var executor = CreateStrictExecutor();
        var entry = new MatchResultEntry(ResultType.Played, 1, 0, PenaltyShootoutHomeGoals: 5);
        Task? task = null;
        var synchronousException = Record.Exception(() =>
        {
            task = executor.CorrectMatchResultFromEntryAsync(MatchId.New(), entry);
        });

        synchronousException.Should().BeNull();
        task!.IsFaulted.Should().BeTrue();
        Func<Task> awaitFaulted = async () => await task!.ConfigureAwait(false);
        (await awaitFaulted.Should().ThrowAsync<DomainException>())
            .Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    private UseCaseExecutor CreateStrictExecutor() =>
        new(
            new Mock<IStageRepository>(MockBehavior.Strict).Object,
            new Mock<IMatchRepository>(MockBehavior.Strict).Object,
            new Mock<ICompetitionRepository>(MockBehavior.Strict).Object,
            new Mock<IUnitOfWork>(MockBehavior.Strict).Object,
            _clock,
            AlwaysExistingMedia.Instance,
            NullLogger<UseCaseExecutor>.Instance);
}
