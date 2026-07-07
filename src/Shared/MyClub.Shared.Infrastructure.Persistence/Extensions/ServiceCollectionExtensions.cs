// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyClub.Shared.Application.Abstractions.ErrorHandling;
using MyClub.Shared.Application.Abstractions.Monitoring;
using MyClub.Shared.Infrastructure.Persistence.ErrorHandling;
using MyClub.Shared.Infrastructure.Persistence.ErrorHandling.Connection;
using MyClub.Shared.Infrastructure.Persistence.ErrorHandling.PersistenceError;
using MyClub.Shared.Infrastructure.Persistence.Monitoring;

namespace MyClub.Shared.Infrastructure.Persistence.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register shared persistence infrastructure
/// components with the dependency injection container.
/// </summary>
/// <remarks>
/// This class provides a centralized registration point for all shared persistence-related services,
/// ensuring proper dependency injection configuration and lifecycle management across all modules.
/// All services follow enterprise patterns with proper configuration validation and monitoring.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all shared persistence error handling services with default configuration.
    /// Includes circuit breaker, retry policies, and error handling strategies.
    /// </summary>
    /// <param name="services">The service collection to add persistence services to.</param>
    /// <param name="configureOptions">Optional action to customize error handling options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddSharedPersistenceServices(
        this IServiceCollection services,
        Action<PersistenceErrorHandlingOptions>? configureOptions = null)
    {
        // Configure error handling options
        var options = new PersistenceErrorHandlingOptions();
        configureOptions?.Invoke(options);

        // Register validation for configuration options
        services.AddSingleton<IValidateOptions<PersistenceErrorHandlingOptions>, PersistenceErrorHandlingOptionsValidator>();
        services.AddSingleton(Options.Create(options));

        // Register core error handling services
        services.AddScoped<IPersistenceErrorHandler, PersistenceErrorHandler>();
        services.AddScoped<IConnectionResilienceService, ConnectionResilienceService>();
        services.AddScoped<IDatabaseRetryPolicy, DatabaseRetryPolicy>();

        // Register default null metrics implementation
        services.AddSingleton(NullPersistenceMetrics.Instance);

        return services;
    }

    /// <summary>
    /// Registers only the connection resilience service with minimal dependencies.
    /// Useful when you only need circuit breaker functionality.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">Action to configure connection resilience options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddConnectionResilience(
        this IServiceCollection services,
        Action<PersistenceErrorHandlingOptions> configureOptions)
    {
        var options = new PersistenceErrorHandlingOptions();
        configureOptions(options);

        services.AddSingleton(Options.Create(options));
        services.AddScoped<IConnectionResilienceService, ConnectionResilienceService>();

        return services;
    }

    /// <summary>
    /// Registers only the database retry policy service.
    /// Useful for lightweight retry scenarios without full error handling.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">Action to configure retry policy options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDatabaseRetryPolicy(
        this IServiceCollection services,
        Action<PersistenceErrorHandlingOptions> configureOptions)
    {
        var options = new PersistenceErrorHandlingOptions();
        configureOptions(options);

        services.AddSingleton(Options.Create(options));
        services.AddScoped<IDatabaseRetryPolicy, DatabaseRetryPolicy>();

        return services;
    }

    /// <summary>
    /// Registers a custom metrics provider for persistence infrastructure monitoring.
    /// Replaces the default null metrics implementation.
    /// </summary>
    /// <typeparam name="TMetrics">The type of metrics provider to register.</typeparam>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="lifetime">The service lifetime for the metrics provider.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddPersistenceMetrics<TMetrics>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TMetrics : class, IPersistenceMetrics
    {
        services.Add(new ServiceDescriptor(typeof(IPersistenceMetrics), typeof(TMetrics), lifetime));
        return services;
    }
}

/// <summary>
/// Validator for persistence error handling options to ensure proper configuration.
/// </summary>
internal sealed class PersistenceErrorHandlingOptionsValidator : IValidateOptions<PersistenceErrorHandlingOptions>
{
    public ValidateOptionsResult Validate(string? name, PersistenceErrorHandlingOptions options)
    {
        var failures = new List<string>();

        if (options.MaxRetryAttempts < 0)
            failures.Add("MaxRetryAttempts must be non-negative");

        if (options.CircuitBreakerFailureThreshold <= 0)
            failures.Add("CircuitBreakerFailureThreshold must be positive");

        if (options.CircuitBreakerTimeoutSeconds <= 0)
            failures.Add("CircuitBreakerTimeoutSeconds must be positive");

        if (options.BaseRetryDelayMs <= 0)
            failures.Add("BaseRetryDelayMs must be positive");

        if (options.MaxRetryDelayMs <= options.BaseRetryDelayMs)
            failures.Add("MaxRetryDelayMs must be greater than BaseRetryDelayMs");

        if (options.CommandTimeoutSeconds <= 0)
            failures.Add("CommandTimeoutSeconds must be positive");

        if (options.ConnectionTimeoutSeconds <= 0)
            failures.Add("ConnectionTimeoutSeconds must be positive");

        if (options.SlowQueryThresholdMs <= 0)
            failures.Add("SlowQueryThresholdMs must be positive");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
