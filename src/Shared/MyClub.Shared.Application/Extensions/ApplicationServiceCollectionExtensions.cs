// -----------------------------------------------------------------------
// <copyright file="ApplicationServiceCollectionExtensions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MyClub.Shared.Application.Behaviors;

namespace MyClub.Shared.Application.Extensions;

/// <summary>
/// Extension methods for configuring application services in the dependency injection container.
/// Provides convenient methods to register MediatR, behaviors, validation, and other application layer services.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Adds all shared application services to the dependency injection container.
    /// This includes MediatR, pipeline behaviors, validation, and AutoMapper configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="assemblies">Additional assemblies to scan for handlers, validators, and mapping profiles.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This method registers services in the correct order and includes all recommended behaviors
    /// for a production-ready application. The pipeline execution order is:
    /// 1. LoggingBehavior - Request/response logging
    /// 2. ValidationBehavior - Request validation
    /// 3. CachingBehavior - Query result caching
    /// 4. PerformanceBehavior - Performance monitoring.
    /// </remarks>
    public static IServiceCollection AddSharedApplication(this IServiceCollection services, params Assembly[] assemblies)
    {
        var allAssemblies = new[] { Assembly.GetExecutingAssembly() }.Concat(assemblies).ToArray();

        // Register MediatR
        services.AddMediatR(cfg =>
        {
            foreach (var assembly in allAssemblies)
            {
                cfg.RegisterServicesFromAssembly(assembly);
            }
        });

        // Register pipeline behaviors in execution order
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        // Register validation
        foreach (var assembly in allAssemblies)
        {
            services.AddValidatorsFromAssembly(assembly);
        }

        // Register AutoMapper
        services.AddAutoMapper(cfg =>
        {
            foreach (var assembly in allAssemblies)
            {
                cfg.AddMaps(assembly);
            }
        });

        // Register memory cache for caching behavior
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// Adds MediatR with custom pipeline behaviors configuration.
    /// Use this method when you need more control over which behaviors to include.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="includeLogging">Whether to include logging behavior.</param>
    /// <param name="includeValidation">Whether to include validation behavior.</param>
    /// <param name="includeCaching">Whether to include caching behavior.</param>
    /// <param name="includePerformance">Whether to include performance monitoring behavior.</param>
    /// <param name="assemblies">Assemblies to scan for handlers and validators.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddMediatRWithBehaviors(
        this IServiceCollection services,
        bool includeLogging = true,
        bool includeValidation = true,
        bool includeCaching = true,
        bool includePerformance = true,
        params Assembly[] assemblies)
    {
        var allAssemblies = new[] { Assembly.GetExecutingAssembly() }.Concat(assemblies).ToArray();

        // Register MediatR
        services.AddMediatR(cfg =>
        {
            foreach (var assembly in allAssemblies)
            {
                cfg.RegisterServicesFromAssembly(assembly);
            }
        });

        // Register behaviors conditionally in correct order
        if (includeLogging)
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        if (includeValidation)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            foreach (var assembly in allAssemblies)
            {
                services.AddValidatorsFromAssembly(assembly);
            }
        }

        if (includeCaching)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
            services.AddMemoryCache();
        }

        if (includePerformance)
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        // Always register AutoMapper
        services.AddAutoMapper(cfg =>
        {
            foreach (var assembly in allAssemblies)
            {
                cfg.AddMaps(assembly);
            }
        });

        return services;
    }

    /// <summary>
    /// Adds custom performance behavior with specific thresholds.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="warningThresholdMs">Warning threshold in milliseconds.</param>
    /// <param name="errorThresholdMs">Error threshold in milliseconds.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddPerformanceBehavior(
        this IServiceCollection services,
        int warningThresholdMs = 500,
        int errorThresholdMs = 2000)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), serviceProvider =>
        {
            var loggerType = typeof(Microsoft.Extensions.Logging.ILogger<>).MakeGenericType(typeof(PerformanceBehavior<,>));
            var logger = serviceProvider.GetRequiredService(loggerType);

            return Activator.CreateInstance(
                typeof(PerformanceBehavior<,>),
                logger,
                warningThresholdMs,
                errorThresholdMs)!;
        });

        return services;
    }
}
