// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using MyClub.Shared.Kernel.Events;

namespace MyClub.Shared.Infrastructure.Events.Extensions;

/// <summary>
/// Extension methods for configuring domain event services.
/// Provides registration for domain event dispatching and handling.
/// Separate from persistence concerns for better modularity.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds domain event services to the service collection.
    /// Registers the domain event dispatcher implementation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Requires MediatR to be registered separately in the consuming project.
    /// </remarks>
    public static IServiceCollection AddDomainEvents(this IServiceCollection services)
    {
        // Register the domain event dispatcher
        // MediatR must be registered by the consuming application
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }
}
