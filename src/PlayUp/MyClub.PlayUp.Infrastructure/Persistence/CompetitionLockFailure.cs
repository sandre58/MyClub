// -----------------------------------------------------------------------
// <copyright file="CompetitionLockFailure.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Npgsql;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Detects PostgreSQL lock contention errors for competition exclusive writes.
/// </summary>
internal static class CompetitionLockFailure
{
    internal const string LockNotAvailableSqlState = "55P03";
    internal const string DeadlockSqlState = "40P01";
    internal const string QueryCanceledSqlState = "57014";

    public static bool IsCompetitionLockContention(Exception exception) =>
        ContainsPostgresState(exception, LockNotAvailableSqlState, DeadlockSqlState);

    public static bool IsQueryCanceled(Exception exception) =>
        ContainsPostgresState(exception, QueryCanceledSqlState);

    private static bool ContainsPostgresState(Exception exception, params string[] sqlStates)
    {
        var set = new HashSet<string>(sqlStates, StringComparer.Ordinal);
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres && set.Contains(postgres.SqlState))
            {
                return true;
            }
        }

        return false;
    }
}
