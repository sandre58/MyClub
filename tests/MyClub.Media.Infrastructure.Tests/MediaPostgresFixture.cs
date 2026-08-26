// -----------------------------------------------------------------------
// <copyright file="MediaPostgresFixture.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.Media.Infrastructure.DependencyInjection;
using MyClub.Media.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace MyClub.Media.Infrastructure.Tests;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit injects the collection fixture through a public test constructor.")]
public sealed class MediaPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18").Build();
    private string? _storageRoot;
    private ServiceProvider? _provider;

    public IServiceScope CreateScope() =>
        _provider is null
            ? throw new InvalidOperationException("Media PostgreSQL fixture is not initialized.")
            : _provider.CreateScope();

    public string StorageRoot =>
        _storageRoot ?? throw new InvalidOperationException("Media PostgreSQL fixture is not initialized.");

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);

        _storageRoot = Path.Combine(Path.GetTempPath(), "myclub-media-pg", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_storageRoot);

        var services = new ServiceCollection();
        services.AddMediaInfrastructure(_container.GetConnectionString(), _storageRoot);
        _provider = services.BuildServiceProvider(validateScopes: true);

        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        await context.Database.MigrateAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_provider is not null)
        {
            await _provider.DisposeAsync().ConfigureAwait(false);
        }

        await _container.DisposeAsync().ConfigureAwait(false);

        if (_storageRoot is not null && Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, recursive: true);
        }
    }
}
