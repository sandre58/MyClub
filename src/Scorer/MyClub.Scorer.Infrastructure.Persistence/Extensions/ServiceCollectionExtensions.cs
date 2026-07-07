// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.Scorer.Domain.CompetitionAggregate.Repositories;
using MyClub.Scorer.Domain.MatchAggregate.Repositories;
using MyClub.Scorer.Domain.MatchdayAggregate.Repositories;
using MyClub.Scorer.Domain.RoundAggregate.Repositories;
using MyClub.Scorer.Domain.StageAggregate.Repositories;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;
using MyClub.Scorer.Infrastructure.Persistence.Repositories;
using MyClub.Shared.Application.Abstractions.ErrorHandling;
using MyClub.Shared.Infrastructure.Persistence.ErrorHandling;
using MyClub.Shared.Infrastructure.Persistence.ErrorHandling.Connection;
using MyClub.Shared.Infrastructure.Persistence.ErrorHandling.PersistenceError;
using MyClub.Shared.Kernel.Persistence;

namespace MyClub.Scorer.Infrastructure.Persistence.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register Scorer persistence infrastructure
/// components with the dependency injection container.
/// </summary>
/// <remarks>
/// This class provides a centralized registration point for all persistence-related services
/// in the Scorer module, ensuring proper dependency injection configuration and lifecycle management.
/// All repositories follow the Repository pattern and inherit from the shared infrastructure base classes.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers persistence services with advanced configuration options including
    /// custom error handling strategies, retry policies, and monitoring settings.
    /// </summary>
    /// <param name="services">The service collection to add persistence services to.</param>
    /// <param name="dbOptions">Configuration action for Entity Framework DbContext options.</param>
    /// <param name="errorHandlingOptions">Configuration action for error handling settings.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> dbOptions,
        Action<PersistenceErrorHandlingOptions>? errorHandlingOptions = null)
    {
        // Configure error handling options
        var options = new PersistenceErrorHandlingOptions();
        errorHandlingOptions?.Invoke(options);
        services.AddSingleton(options);

        // Register domain repository implementations
        // These provide the concrete implementations of repository contracts defined in the domain layer
        // All repositories inherit from the shared Repository base class and follow consistent patterns
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<ICompetitionRepository, CompetitionRepository>();
        services.AddScoped<IMatchdayRepository, MatchdayRepository>();
        services.AddScoped<IRoundRepository, RoundRepository>();
        services.AddScoped<IStageRepository, StageRepository>();

        // Register Entity Framework DbContext with provided configuration
        // The DbContext is registered with scoped lifetime to ensure proper transaction boundaries
        // and change tracking behavior within each request or operation scope
        services.AddDbContext<ScorerDbContext>(dbOptions);

        // Register Unit of Work implementation for transaction management
        // The Unit of Work coordinates changes across multiple repositories and handles
        // domain event dispatching after successful database commits
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register centralized error handling services
        // These provide consistent exception handling and recovery strategies across all repositories
        services.AddSingleton<IPersistenceErrorHandler, PersistenceErrorHandler>();
        services.AddSingleton<IConnectionResilienceService, ConnectionResilienceService>();
        services.AddSingleton<IDatabaseRetryPolicy, DatabaseRetryPolicy>();

        return services;
    }
}
