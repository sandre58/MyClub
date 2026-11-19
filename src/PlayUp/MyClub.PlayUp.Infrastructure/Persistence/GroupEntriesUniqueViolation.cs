// -----------------------------------------------------------------------
// <copyright file="GroupEntriesUniqueViolation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Npgsql;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Detects a PostgreSQL unique violation (23505) that names <c>group_entries</c>.
/// Other constraints, including other 23505 failures, are left untouched.
/// </summary>
internal static class GroupEntriesUniqueViolation
{
    internal static bool IsMatch(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres
                && postgres.SqlState == PostgresErrorCodes.UniqueViolation
                && NamesGroupEntries(postgres))
            {
                return true;
            }
        }

        return false;
    }

    private static bool NamesGroupEntries(PostgresException postgres) =>
        string.Equals(postgres.TableName, "group_entries", StringComparison.Ordinal)
        || (postgres.ConstraintName?.Contains("group_entries", StringComparison.Ordinal) ?? false);
}
