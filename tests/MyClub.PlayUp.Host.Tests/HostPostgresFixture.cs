// -----------------------------------------------------------------------
// <copyright file="HostPostgresFixture.cs" company="Stéphane ANDRE">
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

namespace MyClub.PlayUp.Host.Tests;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit injects the collection fixture through a public test constructor.")]
public sealed class HostPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);

        var services = new ServiceCollection();
        services.AddPlayUpInfrastructure(ConnectionString);
        await using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
        await context.Database.MigrateAsync().ConfigureAwait(false);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
