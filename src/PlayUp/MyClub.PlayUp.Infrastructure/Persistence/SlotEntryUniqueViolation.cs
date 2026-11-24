// -----------------------------------------------------------------------
// <copyright file="SlotEntryUniqueViolation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Npgsql;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Detects a PostgreSQL unique violation (23505) on <c>AK_slots_stage_id_entry_id</c>.
/// Other constraints on <c>slots</c>, including the primary key, are left untouched.
/// </summary>
internal static class SlotEntryUniqueViolation
{
    private const string ConstraintName = "AK_slots_stage_id_entry_id";

    internal static bool IsMatch(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
                && string.Equals(postgres.ConstraintName, ConstraintName, StringComparison.Ordinal)
                && (postgres.TableName is null or "slots"))
            {
                return true;
            }
        }

        return false;
    }
}
