// -----------------------------------------------------------------------
// <copyright file="CompetitionStageIdsInterceptorTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Infrastructure.Persistence.Repositories;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class CompetitionStageIdsInterceptorTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task SetStageOrder_persists_through_unit_of_work_when_competition_stays_unchangedAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var stageA = StageId.New();
        var stageB = StageId.New();
        var stageC = StageId.New();
        CompetitionId id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new CompetitionRepository(context);
            IUnitOfWork unitOfWork = context;

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competition.AddStage(stageA, _clock);
            competition.AddStage(stageB, _clock);
            competition.AddStage(stageC, _clock);
            id = competition.Id;

            repository.Add(competition);
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new CompetitionRepository(context);
            IUnitOfWork unitOfWork = context;

            var competition = await repository.GetByIdForUpdateAsync(id);
            competition.Should().NotBeNull();
            competition.StageIds.Should().Equal(stageA, stageB, stageC);

            competition.SetStageOrder([stageC, stageA, stageB], _clock);
            context.Entry(competition).State.Should().Be(EntityState.Unchanged);

            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new CompetitionRepository(context);
            var reloaded = await repository.GetByIdForUpdateAsync(id);

            reloaded.Should().NotBeNull();
            reloaded.StageIds.Should().Equal(stageC, stageA, stageB);
        }
    }
}
