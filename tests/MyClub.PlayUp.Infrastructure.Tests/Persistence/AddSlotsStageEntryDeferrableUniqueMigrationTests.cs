// -----------------------------------------------------------------------
// <copyright file="AddSlotsStageEntryDeferrableUniqueMigrationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
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
public sealed class AddSlotsStageEntryDeferrableUniqueMigrationTests : IAsyncLifetime
{
    private const string MigrationBeforeSlotsEntryUnique = "20261105135623_MakeMatchParticipantsNullable";

    private const string SlotsEntryUniqueMigration = "20261123220016_AddSlotsStageEntryDeferrableUnique";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .Build();

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync() => await _container.StartAsync().ConfigureAwait(false);

    public async Task DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);

    [IntegrationFact]
    public async Task Up_adds_deferrable_unique_on_stage_id_and_entry_idAsync()
    {
        await MigrateToAsync(SlotsEntryUniqueMigration).ConfigureAwait(false);

        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();

        var rows = await context.Database
            .SqlQueryRaw<ConstraintRow>(
                """
                SELECT
                    c.conname AS "Name",
                    c.condeferrable AS "Deferrable",
                    c.condeferred AS "Deferred",
                    array_to_string(ARRAY_AGG(a.attname ORDER BY u.ordinality), ',') AS "Columns"
                FROM pg_constraint AS c
                JOIN pg_class AS rel ON rel.oid = c.conrelid
                JOIN unnest(c.conkey) WITH ORDINALITY AS u(attnum, ordinality) ON true
                JOIN pg_attribute AS a ON a.attrelid = c.conrelid AND a.attnum = u.attnum
                WHERE rel.relname = 'slots'
                  AND c.contype = 'u'
                  AND c.conname = 'AK_slots_stage_id_entry_id'
                GROUP BY c.conname, c.condeferrable, c.condeferred
                """)
            .ToListAsync()
            .ConfigureAwait(false);

        rows.Should().ContainSingle();
        rows[0].Name.Should().Be("AK_slots_stage_id_entry_id");
        rows[0].Deferrable.Should().BeTrue();
        rows[0].Deferred.Should().BeTrue();
        rows[0].Columns.Should().Be("stage_id,entry_id");
    }

    [IntegrationFact]
    public async Task Down_removes_only_the_slots_entry_unique_constraintAsync()
    {
        await MigrateToAsync(SlotsEntryUniqueMigration).ConfigureAwait(false);

        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(MigrationBeforeSlotsEntryUnique).ConfigureAwait(false);

        var afterDown = await context.Database
            .SqlQueryRaw<ConstraintCountRow>(
                """
                SELECT COUNT(*)::int AS "Count"
                FROM pg_constraint AS c
                JOIN pg_class AS rel ON rel.oid = c.conrelid
                WHERE rel.relname = 'slots'
                  AND c.conname = 'AK_slots_stage_id_entry_id'
                """)
            .SingleAsync()
            .ConfigureAwait(false);
        afterDown.Count.Should().Be(0);

        var pkStillPresent = await context.Database
            .SqlQueryRaw<ConstraintCountRow>(
                """
                SELECT COUNT(*)::int AS "Count"
                FROM pg_constraint AS c
                JOIN pg_class AS rel ON rel.oid = c.conrelid
                WHERE rel.relname = 'slots'
                  AND c.contype = 'p'
                """)
            .SingleAsync()
            .ConfigureAwait(false);
        pkStillPresent.Count.Should().Be(1);

        await migrator.MigrateAsync(SlotsEntryUniqueMigration).ConfigureAwait(false);
        await Up_adds_deferrable_unique_on_stage_id_and_entry_idAsync().ConfigureAwait(false);
    }

    [IntegrationFact]
    public async Task Multiple_vacant_slots_on_the_same_stage_remain_committable_after_UpAsync()
    {
        await MigrateToAsync(SlotsEntryUniqueMigration).ConfigureAwait(false);

        using var scope = CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Vacants"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("A");
        stage.AddSlot("B");
        stage.AddSlot("C");
        competition.AddStage(stage.Id, _clock);
        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync().ConfigureAwait(false);

        stage.FindSlot("A")!.EntryId.Should().BeNull();
        stage.FindSlot("B")!.EntryId.Should().BeNull();
        stage.FindSlot("C")!.EntryId.Should().BeNull();
    }

    [IntegrationFact]
    public async Task ApplyResolvedEntry_move_within_one_SaveChanges_succeeds_after_UpAsync()
    {
        await MigrateToAsync(SlotsEntryUniqueMigration).ConfigureAwait(false);

        using var scope = CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Move"), SampleRegulations.Standard(), _clock);
        var entryId = competition.AddEntry(TeamId.New(), "A", _clock).Id;
        var stage = Stage.Create(competition.Id, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("A");
        stage.AddSlot("B");
        stage.AddResolvedPopulationEntry(entryId, _clock);
        stage.ApplyResolvedEntry("A", entryId, _clock);
        competition.AddStage(stage.Id, _clock);
        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync().ConfigureAwait(false);

        var stageId = stage.Id;
        var reloaded = await stages.GetByIdForUpdateAsync(stageId).ConfigureAwait(false);
        reloaded!.ApplyResolvedEntry("B", entryId, _clock);
        await unitOfWork.SaveChangesAsync().ConfigureAwait(false);

        var verify = await stages.GetByIdForUpdateAsync(stageId).ConfigureAwait(false);
        verify!.FindSlot("A")!.EntryId.Should().BeNull();
        verify.FindSlot("B")!.EntryId.Should().Be(entryId);
    }

    [IntegrationFact]
    public async Task Up_aborts_with_an_explicit_message_when_duplicate_occupied_pairs_existAsync()
    {
        var (competitionId, stageId, entryId) = await SeedStageWithEntryAsync().ConfigureAwait(false);

        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(MigrationBeforeSlotsEntryUnique).ConfigureAwait(false);

        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO slots (stage_id, slot_key, entry_id)
            VALUES ({stageId.Value}, 'DUP-B', {entryId.Value});
            """).ConfigureAwait(false);

        var act = async () => await migrator.MigrateAsync(SlotsEntryUniqueMigration).ConfigureAwait(false);
        var assertion = await act.Should().ThrowAsync<Exception>().ConfigureAwait(false);
        assertion.Which.Message.Should().Contain("AK_slots_stage_id_entry_id");

        var constraintAbsent = await context.Database
            .SqlQueryRaw<ConstraintCountRow>(
                """
                SELECT COUNT(*)::int AS "Count"
                FROM pg_constraint AS c
                JOIN pg_class AS rel ON rel.oid = c.conrelid
                WHERE rel.relname = 'slots'
                  AND c.conname = 'AK_slots_stage_id_entry_id'
                """)
            .SingleAsync()
            .ConfigureAwait(false);
        constraintAbsent.Count.Should().Be(0);

        var duplicateRows = await context.Database
            .SqlQueryRaw<ConstraintCountRow>(
                """
                SELECT COUNT(*)::int AS "Count"
                FROM slots
                WHERE stage_id = {0} AND entry_id = {1}
                """,
                stageId.Value,
                entryId.Value)
            .SingleAsync()
            .ConfigureAwait(false);
        duplicateRows.Count.Should().Be(2);

        await context.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM slots
            WHERE stage_id = {stageId.Value} AND slot_key = 'DUP-B';
            """).ConfigureAwait(false);

        await migrator.MigrateAsync(SlotsEntryUniqueMigration).ConfigureAwait(false);
    }

    private async Task<(CompetitionId CompetitionId, StageId StageId, EntryId EntryId)> SeedStageWithEntryAsync()
    {
        await MigrateToAsync(MigrationBeforeSlotsEntryUnique).ConfigureAwait(false);

        using var scope = CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Guard"), SampleRegulations.Standard(), _clock);
        var entryId = competition.AddEntry(TeamId.New(), "A", _clock).Id;
        var stage = Stage.Create(competition.Id, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("DUP-A");
        stage.AddResolvedPopulationEntry(entryId, _clock);
        stage.ApplyResolvedEntry("DUP-A", entryId, _clock);
        competition.AddStage(stage.Id, _clock);
        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync().ConfigureAwait(false);

        return (competition.Id, stage.Id, entryId);
    }

    private async Task MigrateToAsync(string migrationId)
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(migrationId).ConfigureAwait(false);
    }

    private IServiceScope CreateScope()
    {
        var services = new ServiceCollection();
        services.AddPlayUpInfrastructure(_container.GetConnectionString());
        var provider = services.BuildServiceProvider(validateScopes: true);
        return provider.CreateScope();
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Instantiated by the runtime.")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Migration constraint shape.")]
    private sealed record ConstraintRow(string Name, bool Deferrable, bool Deferred, string Columns);

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Instantiated by the runtime.")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Migration guard shape.")]
    private sealed record ConstraintCountRow(int Count);
}
