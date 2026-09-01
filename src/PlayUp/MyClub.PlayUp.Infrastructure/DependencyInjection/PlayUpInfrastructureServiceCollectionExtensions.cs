// -----------------------------------------------------------------------
// <copyright file="PlayUpInfrastructureServiceCollectionExtensions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Infrastructure.Persistence;
using MyClub.PlayUp.Infrastructure.Persistence.Repositories;
using MyClub.PlayUp.Infrastructure.Persistence.SaveInterceptors;
using MyClub.PlayUp.Infrastructure.Time;

namespace MyClub.PlayUp.Infrastructure.DependencyInjection;

/// <summary>
/// Registers Play'Up infrastructure services for a future Host and for tests.
/// </summary>
public static class PlayUpInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Play'Up DbContext (PostgreSQL), ordered-collection interceptors, repositories, unit of work, and system clock.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddPlayUpInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddSingleton<CompetitionOrderedCollectionsInterceptor>();
        services.AddSingleton<StageOrderedCollectionsInterceptor>();
        services.AddSingleton<MatchOrderedCollectionsInterceptor>();
        services.AddDbContext<PlayUpDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(
                serviceProvider.GetRequiredService<CompetitionOrderedCollectionsInterceptor>(),
                serviceProvider.GetRequiredService<StageOrderedCollectionsInterceptor>(),
                serviceProvider.GetRequiredService<MatchOrderedCollectionsInterceptor>());
        });
        services.AddScoped<IUnitOfWork>(static sp => sp.GetRequiredService<PlayUpDbContext>());
        services.AddScoped<ICompetitionRepository, CompetitionRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IStageRepository, StageRepository>();
        services.AddScoped<IPlayUpDatabaseReset, PlayUpDatabaseReset>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
