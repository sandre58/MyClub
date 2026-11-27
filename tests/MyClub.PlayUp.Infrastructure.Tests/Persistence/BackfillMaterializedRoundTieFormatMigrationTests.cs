// -----------------------------------------------------------------------
// <copyright file="BackfillMaterializedRoundTieFormatMigrationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Infrastructure.DependencyInjection;
using MyClub.PlayUp.Infrastructure.Persistence;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;
using Testcontainers.PostgreSql;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

[Trait("Category", "Integration")]
public sealed class BackfillMaterializedRoundTieFormatMigrationTests : IAsyncLifetime
{
    private const string MigrationBefore = "20261123220016_AddSlotsStageEntryDeferrableUnique";
    private const string TargetMigration = "20261127212737_BackfillMaterializedRoundTieFormat";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .Build();

    public async Task InitializeAsync() => await _container.StartAsync().ConfigureAwait(false);

    public async Task DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);

    [IntegrationFact]
    public async Task Applying_migration_twice_is_idempotentAsync()
    {
        var services = new ServiceCollection();
        services.AddPlayUpInfrastructure(_container.GetConnectionString());
        await using var provider = services.BuildServiceProvider(validateScopes: true);

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(MigrationBefore).ConfigureAwait(false);
        }

        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            await context.Database.MigrateAsync().ConfigureAwait(false);
        }

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var applied = (await context.Database.GetAppliedMigrationsAsync().ConfigureAwait(false))
                .ToArray();
            applied.Should().Contain(TargetMigration);
        }
    }

    [IntegrationFact]
    public async Task Stamped_round_tie_format_matches_default_one_leg_jsonAsync()
    {
        var services = new ServiceCollection();
        services.AddPlayUpInfrastructure(_container.GetConnectionString());
        await using var provider = services.BuildServiceProvider(validateScopes: true);

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(TargetMigration).ConfigureAwait(false);
        }

        var expectedJson = TieFormatJson.Serialize(TieFormat.DefaultOneLeg);

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var rows = await context.Database
                .SqlQueryRaw<string>(
                    """
                    SELECT tie_format AS "Value"
                    FROM rounds
                    WHERE tie_format IS NOT NULL
                    LIMIT 1
                    """)
                .ToListAsync()
                .ConfigureAwait(false);

            if (rows.Count > 0)
            {
                var round = TieFormatJson.Deserialize(rows[0]);
                round.Should().BeEquivalentTo(
                    TieFormat.DefaultOneLeg,
                    options => options.ComparingByMembers<TieFormat>());
                rows[0].Should().Be(expectedJson);
            }
        }
    }
}
