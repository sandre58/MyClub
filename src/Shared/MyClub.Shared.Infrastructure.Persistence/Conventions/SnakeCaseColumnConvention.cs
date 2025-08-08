// -----------------------------------------------------------------------
// <copyright file="SnakeCaseColumnConvention.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace MyClub.Shared.Infrastructure.Persistence.Conventions;

/// <summary>
/// Entity Framework Core model convention for converting property names to snake_case
/// column names in the database. This convention improves database compatibility and
/// follows naming standards common in many database systems and environments.
/// </summary>
/// <remarks>
/// The SnakeCaseColumnConvention transforms C# property names written in PascalCase
/// or camelCase to snake_case database column names, which are widely preferred in
/// database design for their readability and compatibility across different database
/// systems. This convention ensures consistent column naming while maintaining the
/// natural C# property naming conventions in the application code.
/// </remarks>
public static partial class SnakeCaseColumnConvention
{
    /// <summary>
    /// Applies snake_case naming conversion to all entity property column names in the model.
    /// The conversion transforms PascalCase and camelCase property names to lowercase
    /// underscore-separated column names for improved database compatibility.
    /// </summary>
    /// <param name="modelBuilder">The Entity Framework Core model builder to apply conventions to.</param>
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                var name = property.Name;
                var snakeCase = ToSnakeCase(name);
                property.SetColumnName(snakeCase);
            }
        }
    }

    private static string ToSnakeCase(string input) => SnakeCaseRegex().Replace(input, "$1_$2").ToLower(ConventionHelper.DatabaseCulture);

    /// <summary>
    /// Regular expression pattern for identifying case transitions in property names
    /// to enable accurate snake_case conversion. Uses source-generated regex for optimal performance.
    /// </summary>
    /// <returns>A compiled regular expression that matches lowercase/numeric followed by uppercase characters.</returns>
    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex SnakeCaseRegex();
}
