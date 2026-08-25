// -----------------------------------------------------------------------
// <copyright file="DevDatabaseGuard.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Net;
using Npgsql;

namespace MyClub.PlayUp.DevRunner;

/// <summary>
/// Structural safety checks before destructive Development Workspace database operations.
/// </summary>
public static class DevDatabaseGuard
{
    /// <summary>Connection string configuration key for the Development Workspace.</summary>
    public const string ConnectionStringName = "PlayUpDev";

    /// <summary>Required PostgreSQL database name suffix for reset/seed.</summary>
    public const string RequiredDatabaseNameSuffix = "_dev";

    /// <summary>
    /// Validates that the connection string targets a Development Workspace database
    /// that may be reset or seeded from DevRunner.
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="environmentName">Logical environment name (must not be Production).</param>
    public static void ValidateForDestructiveUse(string connectionString, string environmentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "DevRunner destructive operations are not allowed when environment is Production.");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' has no Database name.");
        }

        if (!databaseName.EndsWith(RequiredDatabaseNameSuffix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"PostgreSQL reset/seed refused: database '{databaseName}' must end with '{RequiredDatabaseNameSuffix}'."));
        }

        var host = builder.Host;
        if (string.IsNullOrWhiteSpace(host) || !IsLocalHost(host))
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"PostgreSQL reset/seed refused: host '{host}' is not a local address (localhost / loopback)."));
        }
    }

    private static bool IsLocalHost(string host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || host is "127.0.0.1" or "::1" or "[::1]" || (IPAddress.TryParse(host.Trim('[', ']'), out var address) && IPAddress.IsLoopback(address));
}
