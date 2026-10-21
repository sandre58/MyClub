// -----------------------------------------------------------------------
// <copyright file="HostPostgresFixture.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.Media.Infrastructure.DependencyInjection;
using MyClub.Media.Infrastructure.Persistence;
using MyClub.PlayUp.Infrastructure.DependencyInjection;
using MyClub.PlayUp.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit injects the collection fixture through a public test constructor.")]
public sealed class HostPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .Build();

    private readonly string _mediaStorageRoot = Path.Combine(
        Path.GetTempPath(),
        "myclub-media-host-fixture",
        Guid.CreateVersion7().ToString("N"));

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);
        Directory.CreateDirectory(_mediaStorageRoot);

        var services = new ServiceCollection();
        services.AddPlayUpInfrastructure(ConnectionString);
        services.AddMediaInfrastructure(ConnectionString, _mediaStorageRoot);
        await using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
        await context.Database.MigrateAsync().ConfigureAwait(false);
        var media = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        await media.Database.MigrateAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync().ConfigureAwait(false);
        if (Directory.Exists(_mediaStorageRoot))
        {
            Directory.Delete(_mediaStorageRoot, recursive: true);
        }
    }
}
