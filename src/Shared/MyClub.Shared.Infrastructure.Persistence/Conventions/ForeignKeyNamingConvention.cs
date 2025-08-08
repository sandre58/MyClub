// -----------------------------------------------------------------------
// <copyright file="ForeignKeyNamingConvention.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace MyClub.Shared.Infrastructure.Persistence.Conventions;

/// <summary>
/// Entity Framework Core model convention for standardizing foreign key constraint naming
/// across all entities in the database schema. This convention ensures consistent and
/// descriptive foreign key names that improve database maintainability and debugging.
/// </summary>
/// <remarks>
/// The ForeignKeyNamingConvention applies a standardized naming pattern to all foreign key
/// constraints in the database, creating names that clearly identify the relationship between
/// tables and the specific properties involved in the constraint. This improves database
/// administration, debugging, and cross-database compatibility by providing predictable
/// constraint names that follow a consistent format across the entire schema.
/// </remarks>
public static class ForeignKeyNamingConvention
{
    /// <summary>
    /// Applies standardized foreign key constraint naming to all entities in the model.
    /// The naming pattern follows the format: FK_{DependentTable}_{PrincipalTable}_{PropertyNames}.
    /// </summary>
    /// <param name="modelBuilder">The Entity Framework Core model builder to apply conventions to.</param>
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var fkName = $"FK_{entity.GetTableName()}_{foreignKey.PrincipalEntityType.GetTableName()}_{string.Join("_", foreignKey.Properties.Select(static p => p.Name))}";
                foreignKey.SetConstraintName(fkName);
            }
        }
    }
}
