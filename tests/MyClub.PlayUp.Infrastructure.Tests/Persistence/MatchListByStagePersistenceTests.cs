// -----------------------------------------------------------------------
// <copyright file="MatchListByStagePersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Infrastructure.Persistence.Repositories;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class MatchListByStagePersistenceTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 22, 40, 0, TimeSpan.Zero));

    [Fact]
    public async Task ListByStage_filters_orders_and_returns_empty_when_noneAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var stageA = StageId.New();
        var stageB = StageId.New();
        var competitionId = CompetitionId.New();

        MatchId firstId;
        MatchId secondId;
        MatchId otherStageId;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new MatchRepository(context);
            IUnitOfWork unitOfWork = context;

            var first = Match.Create(competitionId, stageA, EntryId.New(), EntryId.New(), _clock);
            var second = Match.Create(competitionId, stageA, EntryId.New(), EntryId.New(), _clock);
            var other = Match.Create(competitionId, stageB, EntryId.New(), EntryId.New(), _clock);
            firstId = first.Id;
            secondId = second.Id;
            otherStageId = other.Id;

            repository.Add(second);
            repository.Add(other);
            repository.Add(first);
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new MatchRepository(context);
            var listed = await repository.ListByStageForUpdateAsync(stageA);

            listed.Should().HaveCount(2);
            listed.Select(match => match.Id).Should().Equal(
                new[] { firstId, secondId }.OrderBy(id => id.Value));
            listed.Should().OnlyContain(match => match.StageId.Equals(stageA));
            listed.Should().NotContain(match => match.Id.Equals(otherStageId));

            (await repository.ListByStageForUpdateAsync(StageId.New())).Should().BeEmpty();
        }
    }
}
