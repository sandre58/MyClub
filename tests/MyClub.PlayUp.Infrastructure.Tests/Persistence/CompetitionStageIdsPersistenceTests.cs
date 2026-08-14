// -----------------------------------------------------------------------
// <copyright file="CompetitionStageIdsPersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Infrastructure.Persistence;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

[Collection("postgres")]
[Trait("Category", "Integration")]
public sealed class CompetitionStageIdsPersistenceTests(PostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 10, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task SetStageOrder_persists_through_unit_of_work_when_competition_stays_unchangedAsync()
    {
        var stageA = StageId.New();
        var stageB = StageId.New();
        var stageC = StageId.New();
        CompetitionId id;

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competition.AddStage(stageA, _clock);
            competition.AddStage(stageB, _clock);
            competition.AddStage(stageC, _clock);
            id = competition.Id;

            repository.Add(competition);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = await repository.GetByIdAsync(id);
            competition.Should().NotBeNull();
            competition.StageIds.Should().Equal(stageA, stageB, stageC);

            competition.SetStageOrder([stageC, stageA, stageB], _clock);
            context.Entry(competition).State.Should().Be(EntityState.Unchanged);

            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var reloaded = await repository.GetByIdAsync(id);

            reloaded.Should().NotBeNull();
            reloaded.StageIds.Should().Equal(stageC, stageA, stageB);
        }
    }
}
