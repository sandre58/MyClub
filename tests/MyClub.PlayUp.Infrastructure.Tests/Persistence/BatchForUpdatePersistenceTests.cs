// -----------------------------------------------------------------------
// <copyright file="BatchForUpdatePersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence.Repositories;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

/// <summary>
/// Batch ForUpdate ports load multiple ARs without per-id loops.
/// </summary>
public sealed class BatchForUpdatePersistenceTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 29, 15, 30, 0, TimeSpan.Zero));

    [Fact]
    public async Task GetByIdsForUpdate_stages_preserves_caller_order_and_skips_missingAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var competitionId = CompetitionId.New();
        StageId firstId;
        StageId secondId;
        var missingId = StageId.New();

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new StageRepository(context);
            IUnitOfWork unitOfWork = context;

            var first = Stage.Create(competitionId, new StageName("A"), SampleRegulations.Standard(), _clock);
            var second = Stage.Create(competitionId, new StageName("B"), SampleRegulations.Standard(), _clock);
            firstId = first.Id;
            secondId = second.Id;
            repository.Add(first);
            repository.Add(second);
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new StageRepository(context);
            var loaded = await repository.GetByIdsForUpdateAsync([secondId, missingId, firstId]);

            loaded.Select(stage => stage.Id).Should().Equal(secondId, firstId);
            foreach (var stage in loaded)
            {
                context.Entry(stage).State.Should().NotBe(Microsoft.EntityFrameworkCore.EntityState.Detached);
            }
        }
    }

    [Fact]
    public async Task GetByIdsForUpdate_stages_empty_ids_returns_emptyAsync()
    {
        await using var context = PlayUpInMemory.CreateContext();
        var repository = new StageRepository(context);

        (await repository.GetByIdsForUpdateAsync([])).Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdsForUpdate_stages_tracks_for_mutation_roundtripAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var competitionId = CompetitionId.New();
        StageId stageId;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new StageRepository(context);
            IUnitOfWork unitOfWork = context;
            var stage = Stage.Create(competitionId, new StageName("QF"), SampleRegulations.Standard(), _clock);
            stageId = stage.Id;
            repository.Add(stage);
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new StageRepository(context);
            IUnitOfWork unitOfWork = context;
            var loaded = await repository.GetByIdsForUpdateAsync([stageId]);
            loaded.Should().ContainSingle();
            loaded[0].AddRound("R1", _clock);
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new StageRepository(context).GetByIdForUpdateAsync(stageId);
            reloaded.Should().NotBeNull();
            reloaded.Rounds.Should().ContainSingle().Which.Name.Should().Be("R1");
        }
    }

    [Fact]
    public async Task GetByIdsForUpdate_matches_preserves_order_and_skips_missingAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var competitionId = CompetitionId.New();
        var stageId = StageId.New();
        MatchId firstId;
        MatchId secondId;
        var missingId = MatchId.New();

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new MatchRepository(context);
            IUnitOfWork unitOfWork = context;
            var first = Match.Create(competitionId, stageId, EntryId.New(), EntryId.New(), _clock);
            var second = Match.Create(competitionId, stageId, EntryId.New(), EntryId.New(), _clock);
            firstId = first.Id;
            secondId = second.Id;
            repository.Add(first);
            repository.Add(second);
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new MatchRepository(context);
            var loaded = await repository.GetByIdsForUpdateAsync([secondId, missingId, firstId]);

            loaded.Select(match => match.Id).Should().Equal(secondId, firstId);
        }
    }

    [Fact]
    public async Task ListByStageIdsForUpdate_groups_matches_by_stageAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var competitionId = CompetitionId.New();
        var stageA = StageId.New();
        var stageB = StageId.New();
        var stageEmpty = StageId.New();
        MatchId a1;
        MatchId a2;
        MatchId b1;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new MatchRepository(context);
            IUnitOfWork unitOfWork = context;
            var matchA1 = Match.Create(competitionId, stageA, EntryId.New(), EntryId.New(), _clock);
            var matchA2 = Match.Create(competitionId, stageA, EntryId.New(), EntryId.New(), _clock);
            var matchB1 = Match.Create(competitionId, stageB, EntryId.New(), EntryId.New(), _clock);
            a1 = matchA1.Id;
            a2 = matchA2.Id;
            b1 = matchB1.Id;
            repository.Add(matchA2);
            repository.Add(matchB1);
            repository.Add(matchA1);
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new MatchRepository(context);
            var byStage = await repository.ListByStageIdsForUpdateAsync([stageA, stageEmpty, stageB]);

            byStage.Should().HaveCount(2);
            byStage.Should().ContainKey(stageA);
            byStage.Should().ContainKey(stageB);
            byStage.Should().NotContainKey(stageEmpty);
            byStage[stageA].Select(match => match.Id).Should().Equal(
                new[] { a1, a2 }.OrderBy(id => id.Value));
            byStage[stageB].Select(match => match.Id).Should().Equal(b1);
        }
    }
}
