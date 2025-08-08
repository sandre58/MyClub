// -----------------------------------------------------------------------
// <copyright file="ModelBuilderExtensions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using MyClub.Shared.Infrastructure.Persistence.Conventions;

namespace MyClub.Shared.Infrastructure.Persistence.Extensions;

/// <summary>
/// Extension methods for Entity Framework Core ModelBuilder to apply all database conventions
/// in a coordinated manner. This class provides the central application point for all
/// persistence layer conventions ensuring consistent database schema generation.
/// </summary>
/// <remarks>
/// The ModelBuilderExtensions centralizes convention application to ensure that all database
/// schema conventions are applied consistently and in the correct order. This approach
/// provides a single point of control for database schema generation while maintaining
/// separation of concerns between different convention types and their specific responsibilities.
/// </remarks>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies all database conventions to the Entity Framework Core model in the correct order.
    /// This method coordinates the application of naming conventions, structural conventions,
    /// and relationship conventions to ensure a consistent and well-formed database schema.
    /// </summary>
    /// <param name="modelBuilder">The Entity Framework Core model builder to apply conventions to.</param>
    public static void ApplyConventions(this ModelBuilder modelBuilder)
    {
        PluralizeTableNameConvention.Apply(modelBuilder);
        ForeignKeyNamingConvention.Apply(modelBuilder);
    }
}
