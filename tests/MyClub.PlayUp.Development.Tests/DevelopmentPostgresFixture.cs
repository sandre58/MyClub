// -----------------------------------------------------------------------
// <copyright file="DevelopmentPostgresFixture.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Infrastructure.DependencyInjection;
using MyClub.PlayUp.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit injects the collection fixture through a public test constructor.")]
public sealed class DevelopmentPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18").Build();
    private ServiceProvider? _provider;

    public IServiceProvider Services =>
        _provider ?? throw new InvalidOperationException("PostgreSQL fixture is not initialized.");

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);

        var adminBuilder = new Npgsql.NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Pooling = false
        };
        const string databaseName = "playup_development_tests";
        await using (var admin = new Npgsql.NpgsqlConnection(adminBuilder.ConnectionString))
        {
            await admin.OpenAsync().ConfigureAwait(false);
            await using var create = new Npgsql.NpgsqlCommand(
                $"CREATE DATABASE \"{databaseName}\";",
                admin);
            try
            {
                await create.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P04")
            {
                // database already exists
            }
        }

        adminBuilder.Database = databaseName;
        var connectionString = adminBuilder.ConnectionString;

        var services = new ServiceCollection();
        services.AddPlayUpInfrastructure(connectionString);
        services.AddSingleton<IWorkspaceStore, FixturePostgresWorkspaceStore>();
        services.AddPlayUpDevelopmentWorkspace(new DevelopmentWorkspaceOptions { Seed = 42 });
        services.AddScoped<Application.Pipeline.UseCaseExecutor>();
        services.AddSingleton<Domain.Common.IClock>(_ =>
            new FakeClock(new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero)));
        _provider = services.BuildServiceProvider(validateScopes: true);

        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
        await context.Database.MigrateAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_provider is not null)
        {
            await _provider.DisposeAsync().ConfigureAwait(false);
        }

        await _container.DisposeAsync().ConfigureAwait(false);
    }

    private sealed class FixturePostgresWorkspaceStore(IServiceScopeFactory scopeFactory) : IWorkspaceStore
    {
        public async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            var scope = scopeFactory.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
                var connectionString = context.Database.GetConnectionString()
                    ?? throw new InvalidOperationException("Missing connection string.");
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
                var databaseName = builder.Database
                    ?? throw new InvalidOperationException("Missing database name.");
                builder.Database = "postgres";

                Npgsql.NpgsqlConnection.ClearAllPools();
                await using (var connection = new Npgsql.NpgsqlConnection(builder.ConnectionString))
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
#pragma warning disable CA2100
                    await using var command = new Npgsql.NpgsqlCommand(
                        $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE); CREATE DATABASE \"{databaseName}\";",
                        connection);
#pragma warning restore CA2100
                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

[CollectionDefinition("DevelopmentPostgres")]
[SuppressMessage("Design", "CA1515", Justification = "xUnit collection definition must be public.")]
[SuppressMessage("Naming", "CA1711", Justification = "xUnit collection naming convention.")]
public sealed class DevelopmentPostgresCollectionDefinition : ICollectionFixture<DevelopmentPostgresFixture>;
