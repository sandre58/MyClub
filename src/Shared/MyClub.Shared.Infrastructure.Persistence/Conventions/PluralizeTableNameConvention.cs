// -----------------------------------------------------------------------
// <copyright file="PluralizeTableNameConvention.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using MyNet.Humanizer;

namespace MyClub.Shared.Infrastructure.Persistence.Conventions;

/// <summary>
/// Entity Framework Core model convention for automatically pluralizing table names
/// based on entity class names. This convention follows database naming best practices
/// by ensuring table names represent collections of entities rather than single instances.
/// </summary>
/// <remarks>
/// The PluralizeTableNameConvention applies standard English pluralization rules to
/// entity class names to generate appropriate table names, following common database
/// design conventions where table names should be plural to indicate they contain
/// multiple rows/entities. This improves database schema readability and follows
/// widely accepted naming standards across different database systems and ORMs.
/// </remarks>
public static class PluralizeTableNameConvention
{
    /// <summary>
    /// Applies pluralization to all entity table names in the model using English language rules.
    /// The pluralization follows standard linguistic patterns to convert singular entity names
    /// to appropriate plural table names for database storage.
    /// </summary>
    /// <param name="modelBuilder">The Entity Framework Core model builder to apply conventions to.</param>
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var pluralName = entity.ClrType.Name.Pluralize(culture: ConventionHelper.DatabaseCulture);
            entity.SetTableName(pluralName);
        }
    }
}
