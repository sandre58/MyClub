// -----------------------------------------------------------------------
// <copyright file="ConventionHelper.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;

namespace MyClub.Shared.Infrastructure.Persistence.Conventions;

/// <summary>
/// Static helper class providing shared constants and utilities for Entity Framework Core
/// model conventions. This class centralizes common configuration values and cultural
/// settings used across all database conventions to ensure consistency.
/// </summary>
/// <remarks>
/// The ConventionHelper provides standardized configuration values that ensure consistent
/// behavior across all Entity Framework Core conventions, particularly for database naming,
/// cultural formatting, and data separation patterns. This centralization improves
/// maintainability and ensures uniform database schema generation regardless of the
/// specific convention being applied.
/// </remarks>
public static class ConventionHelper
{
    /// <summary>
    /// Gets the standardized culture information used for database operations and naming conventions.
    /// Set to English (en) culture to ensure consistent database naming across different environments.
    /// </summary>
    /// <remarks>
    /// This culture setting ensures that database names, table names, and other database artifacts
    /// are generated consistently regardless of the server's regional settings or the developer's
    /// locale. Using English culture provides maximum compatibility across different database
    /// providers and deployment environments while avoiding localization issues in schema generation.
    /// </remarks>
    public static CultureInfo DatabaseCulture { get; } = CultureInfo.GetCultureInfo("en");

    /// <summary>
    /// Gets the default character used to separate list elements in delimited string storage.
    /// This semicolon separator provides reliable data separation for collection storage scenarios.
    /// </summary>
    /// <remarks>
    /// The semicolon separator is chosen for its reliability in data storage scenarios where
    /// commas might appear within the actual data values. This consistent separator choice
    /// across all conventions ensures predictable behavior in list serialization and
    /// deserialization operations throughout the persistence layer.
    /// </remarks>
    public const char DefaultListSeparator = ';';
}
