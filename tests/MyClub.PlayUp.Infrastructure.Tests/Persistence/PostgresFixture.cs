// -----------------------------------------------------------------------
// <copyright file="PostgresFixture.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Infrastructure.DependencyInjection;
using MyClub.PlayUp.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit injects the collection fixture through a public test constructor.")]
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private ServiceProvider? _provider;

    public IServiceScope CreateScope() => _provider is null ? throw new InvalidOperationException("PostgreSQL fixture is not initialized.") : _provider.CreateScope();

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);

        var services = new ServiceCollection();
        services.AddPlayUpInfrastructure(_container.GetConnectionString());
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
}
