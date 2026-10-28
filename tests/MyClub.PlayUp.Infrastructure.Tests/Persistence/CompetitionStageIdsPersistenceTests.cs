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
        StageId stageA;
        StageId stageB;
        StageId stageC;
        CompetitionId id;

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            id = competition.Id;
            repository.Add(competition);
            await unitOfWork.SaveChangesAsync();

            var stageAEntity = StageSeed.CreateDraft(id, _clock, "A");
            var stageBEntity = StageSeed.CreateDraft(id, _clock, "B");
            var stageCEntity = StageSeed.CreateDraft(id, _clock, "C");
            stageA = stageAEntity.Id;
            stageB = stageBEntity.Id;
            stageC = stageCEntity.Id;
            stages.Add(stageAEntity);
            stages.Add(stageBEntity);
            stages.Add(stageCEntity);
            await unitOfWork.SaveChangesAsync();

            var tracked = await repository.GetByIdForUpdateAsync(id);
            tracked.Should().NotBeNull();
            tracked.AddStage(stageA, _clock);
            tracked.AddStage(stageB, _clock);
            tracked.AddStage(stageC, _clock);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = await repository.GetByIdForUpdateAsync(id);
            competition.Should().NotBeNull();
            competition.StageIds.Should().Equal(stageA, stageB, stageC);

            competition.SetStageOrder([stageC, stageA, stageB], _clock);
            context.Entry(competition).State.Should().Be(EntityState.Unchanged);

            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var reloaded = await repository.GetByIdForUpdateAsync(id);

            reloaded.Should().NotBeNull();
            reloaded.StageIds.Should().Equal(stageC, stageA, stageB);
        }
    }

    [IntegrationFact]
    public async Task Birth_and_SetStageOrder_in_one_save_persists_stage_and_positionAsync()
    {
        StageId stageA;
        StageId stageB;
        StageId stageC;
        CompetitionId id;

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Insert"), SampleRegulations.Standard(), _clock);
            id = competition.Id;
            repository.Add(competition);

            var stageAEntity = StageSeed.CreateDraft(id, _clock, "A");
            var stageBEntity = StageSeed.CreateDraft(id, _clock, "B");
            stageA = stageAEntity.Id;
            stageB = stageBEntity.Id;
            stages.Add(stageAEntity);
            stages.Add(stageBEntity);
            competition.AddStage(stageA, _clock);
            competition.AddStage(stageB, _clock);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = await repository.GetByIdForUpdateAsync(id);
            competition.Should().NotBeNull();
            var stageCEntity = StageSeed.CreateDraft(id, _clock, "C");
            stageC = stageCEntity.Id;
            stages.Add(stageCEntity);
            competition.AddStage(stageC, _clock);
            competition.SetStageOrder([stageC, stageA, stageB], _clock);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var reloaded = await repository.GetByIdForUpdateAsync(id);
            var persisted = await stages.GetByIdForUpdateAsync(stageC);

            reloaded.Should().NotBeNull();
            reloaded.StageIds.Should().Equal(stageC, stageA, stageB);
            persisted.Should().NotBeNull();
            persisted.Name.Value.Should().Be("C");
        }
    }

    [IntegrationFact]
    public async Task Uncommitted_stage_birth_and_order_leaves_no_stage_rowAsync()
    {
        StageId stageA;
        StageId stageB;
        StageId ghost;
        CompetitionId id;

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Atomic"), SampleRegulations.Standard(), _clock);
            id = competition.Id;
            repository.Add(competition);

            var stageAEntity = StageSeed.CreateDraft(id, _clock, "A");
            var stageBEntity = StageSeed.CreateDraft(id, _clock, "B");
            stageA = stageAEntity.Id;
            stageB = stageBEntity.Id;
            stages.Add(stageAEntity);
            stages.Add(stageBEntity);
            competition.AddStage(stageA, _clock);
            competition.AddStage(stageB, _clock);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();

            var competition = await repository.GetByIdForUpdateAsync(id);
            competition.Should().NotBeNull();
            var ghostStage = StageSeed.CreateDraft(id, _clock, "Ghost");
            ghost = ghostStage.Id;
            stages.Add(ghostStage);
            competition.AddStage(ghost, _clock);
            competition.SetStageOrder([ghost, stageA, stageB], _clock);
        }

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var reloaded = await repository.GetByIdForUpdateAsync(id);
            var missing = await stages.GetByIdForUpdateAsync(ghost);

            reloaded.Should().NotBeNull();
            reloaded.StageIds.Should().Equal(stageA, stageB);
            missing.Should().BeNull();
        }
    }
}
