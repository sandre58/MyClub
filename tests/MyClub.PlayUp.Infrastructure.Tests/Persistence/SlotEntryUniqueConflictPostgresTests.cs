// -----------------------------------------------------------------------
// <copyright file="SlotEntryUniqueConflictPostgresTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.DependencyInjection;
using MyClub.PlayUp.Infrastructure.Persistence;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Testcontainers.PostgreSql;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

[Trait("Category", "Integration")]
public sealed class SlotEntryUniqueConflictPostgresTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .Build();

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 14, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync() => await _container.StartAsync().ConfigureAwait(false);

    public async Task DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);

    [IntegrationFact]
    public async Task Duplicate_entry_on_two_slots_maps_to_Application_SlotEntryUniqueConflictAsync()
    {
        using var scope = CreateMigratedScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();

        var competition = Competition.Create(new CompetitionName("Dup"), SampleRegulations.Standard(), _clock);
        var entryId = competition.AddEntry(TeamId.New(), "Alpha", _clock).Id;
        var stage = Stage.Create(competition.Id, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("A");
        stage.AddSlot("B");
        stage.AddResolvedPopulationEntry(entryId, _clock);
        competition.AddStage(stage.Id, _clock);
        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync().ConfigureAwait(false);

        var stageId = stage.Id;
        var reloaded = await stages.GetByIdForUpdateAsync(stageId).ConfigureAwait(false);
        context.Entry(reloaded!.FindSlot("A")!).Property(nameof(Slot.EntryId)).CurrentValue = entryId;
        context.Entry(reloaded.FindSlot("B")!).Property(nameof(Slot.EntryId)).CurrentValue = entryId;

        var act = async () => await unitOfWork.SaveChangesAsync().ConfigureAwait(false);
        var assertion = await act.Should().ThrowAsync<ApplicationFailureException>().ConfigureAwait(false);
        assertion.Which.Code.Should().Be(ApplicationErrorCodes.SlotEntryUniqueConflict);
    }

    [IntegrationFact]
    public async Task ApplyResolvedEntry_move_within_one_SaveChanges_still_succeeds_with_mapper_registeredAsync()
    {
        using var scope = CreateMigratedScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Move"), SampleRegulations.Standard(), _clock);
        var entryId = competition.AddEntry(TeamId.New(), "Alpha", _clock).Id;
        var stage = Stage.Create(competition.Id, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("A");
        stage.AddSlot("B");
        stage.AddResolvedPopulationEntry(entryId, _clock);
        stage.ApplyResolvedEntry("A", entryId, _clock);
        competition.AddStage(stage.Id, _clock);
        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync().ConfigureAwait(false);

        var reloaded = await stages.GetByIdForUpdateAsync(stage.Id).ConfigureAwait(false);
        reloaded!.ApplyResolvedEntry("B", entryId, _clock);
        await unitOfWork.SaveChangesAsync().ConfigureAwait(false);

        reloaded.FindSlot("A")!.EntryId.Should().BeNull();
        reloaded.FindSlot("B")!.EntryId.Should().Be(entryId);
    }

    private IServiceScope CreateMigratedScope()
    {
        var services = new ServiceCollection();
        services.AddPlayUpInfrastructure(_container.GetConnectionString());
        var provider = services.BuildServiceProvider(validateScopes: true);
        var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
        context.Database.Migrate();
        return scope;
    }
}
