// -----------------------------------------------------------------------
// <copyright file="AddResolutionProvenanceMigrationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
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
public sealed class AddResolutionProvenanceMigrationTests : IAsyncLifetime
{
    private const string MigrationBeforeProvenance = "20261030120346_AddDrawInputsIntent";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .Build();

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 30, 8, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync() => await _container.StartAsync().ConfigureAwait(false);

    public async Task DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);

    [IntegrationFact]
    public async Task Upgrade_from_previous_migration_adds_nullable_provenance_columnsAsync()
    {
        var services = new ServiceCollection();
        services.AddPlayUpInfrastructure(_container.GetConnectionString());
        await using var provider = services.BuildServiceProvider(validateScopes: true);

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(MigrationBeforeProvenance).ConfigureAwait(false);
        }

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            await context.Database.MigrateAsync().ConfigureAwait(false);

            var slotColumns = await context.Database
                .SqlQueryRaw<ColumnRow>(
                    """
                    SELECT column_name AS "Name", data_type AS "DataType", is_nullable AS "IsNullable"
                    FROM information_schema.columns
                    WHERE table_name = 'slots'
                      AND column_name = 'resolution_provenance_kind'
                    """)
                .ToListAsync()
                .ConfigureAwait(false);

            slotColumns.Should().ContainSingle();
            slotColumns[0].IsNullable.Should().Be("YES");
        }
    }

    [IntegrationFact]
    public async Task Existing_slot_rows_remain_readable_after_full_migrationAsync()
    {
        using var scope = CreateMigratedScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stageRepository = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Legacy"), SampleRegulations.Standard(), _clock);
        var entryId = competition.AddEntry(TeamId.New(), "A", _clock).Id;
        var stage = Stage.Create(competition.Id, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        const string slotKey = "SF1-A";
        stage.AddSlot(slotKey);
        stage.AddResolvedPopulationEntry(entryId, _clock);
        stage.ApplyResolvedEntry(slotKey, entryId, _clock);
        competition.AddStage(stage.Id, _clock);
        repository.Add(competition);
        stageRepository.Add(stage);
        await unitOfWork.SaveChangesAsync().ConfigureAwait(false);

        var stageId = stage.Id;

        using var verifyScope = CreateMigratedScope();
        var stages = verifyScope.ServiceProvider.GetRequiredService<IStageRepository>();
        var reloaded = await stages.GetByIdForUpdateAsync(stageId).ConfigureAwait(false);
        reloaded!.FindSlot(slotKey)!.EntryId.Should().Be(entryId);
        reloaded.CompositionEntries.Should().ContainSingle().Which.EntryId.Should().Be(entryId);
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

    private sealed record ColumnRow(string Name, string DataType, string IsNullable);
}
