// -----------------------------------------------------------------------
// <copyright file="MediaInfrastructureServiceCollectionExtensions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.Media.Application.Abstractions;
using MyClub.Media.Application.Media;
using MyClub.Media.Infrastructure.Persistence;
using MyClub.Media.Infrastructure.Persistence.Repositories;
using MyClub.Media.Infrastructure.Storage;

namespace MyClub.Media.Infrastructure.DependencyInjection;

/// <summary>
/// Registers Media infrastructure services for Host composition and tests.
/// </summary>
public static class MediaInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Adds Media DbContext (PostgreSQL schema <c>media</c>), local file storage, repository, and application service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">PostgreSQL connection string for Media metadata.</param>
    /// <param name="storageRoot">Local filesystem root for Media binaries.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddMediaInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string storageRoot)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageRoot);

        services.AddDbContext<MediaDbContext>(options => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "media")));

        services.AddSingleton<IMediaStorage>(_ => new LocalFileMediaStorage(storageRoot));
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<MediaService>();

        return services;
    }
}
