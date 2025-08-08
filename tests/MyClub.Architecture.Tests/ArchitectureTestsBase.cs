// -----------------------------------------------------------------------
// <copyright file="ArchitectureTestsBase.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;
using MyClub.Shared.Application.Behaviors;
using MyClub.Shared.Infrastructure.Persistence.Repositories;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Architecture.Tests;

/// <summary>
/// Base class for architecture tests providing common assemblies and utilities.
/// Centralizes assembly references and common test setup.
/// </summary>
public abstract class ArchitectureTestsBase
{
    /// <summary>
    /// Assembly containing domain entities and aggregates.
    /// </summary>
    protected static readonly Assembly DomainAssembly = typeof(Competition).Assembly;

    /// <summary>
    /// Assembly containing application use cases and handlers.
    /// </summary>
    protected static readonly Assembly ApplicationAssembly = typeof(Scorer.Application.Competitions.Commands.AddTeam.AddTeamCommand).Assembly;

    /// <summary>
    /// Assembly containing infrastructure implementations.
    /// </summary>
    protected static readonly Assembly InfrastructureAssembly = typeof(ScorerDbContext).Assembly;

    /// <summary>
    /// Assembly containing shared kernel primitives.
    /// </summary>
    protected static readonly Assembly SharedKernelAssembly = typeof(Entity<>).Assembly;

    /// <summary>
    /// Assembly containing shared domain value objects.
    /// </summary>
    protected static readonly Assembly SharedDomainAssembly = typeof(Shared.Domain.ValueObjects.DisplayName).Assembly;

    /// <summary>
    /// Assembly containing shared application behaviors.
    /// </summary>
    protected static readonly Assembly SharedApplicationAssembly = typeof(ValidationBehavior<,>).Assembly;

    /// <summary>
    /// Assembly containing shared infrastructure implementations.
    /// </summary>
    protected static readonly Assembly SharedInfrastructureAssembly = typeof(RepositoryBase<,,>).Assembly;

    /// <summary>
    /// Common external dependencies that Domain should not reference.
    /// </summary>
    protected static readonly string[] InfrastructureDependencies =
    [
        "Microsoft.EntityFrameworkCore",
        "System.Data.SqlClient",
        "Microsoft.Data.SqlClient",
        "Npgsql",
        "MySql.Data",
        "System.Net.Http",
        "Microsoft.AspNetCore",
        "Newtonsoft.Json",
        "System.Text.Json"
    ];

    /// <summary>
    /// Application framework dependencies that Domain should not reference.
    /// </summary>
    protected static readonly string[] ApplicationFrameworkDependencies =
    [
        "AutoMapper",
        "MediatR",
        "FluentValidation",
        "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.Logging"
    ];
}
