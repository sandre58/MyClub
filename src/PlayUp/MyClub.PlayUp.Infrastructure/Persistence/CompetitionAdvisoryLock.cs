// -----------------------------------------------------------------------
// <copyright file="CompetitionAdvisoryLock.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Derives the PostgreSQL advisory lock key for a competition write.
/// </summary>
public static class CompetitionAdvisoryLock
{
    /// <summary>
    /// Computes the same 64-bit key used by <c>pg_advisory_xact_lock</c> in exclusive writes.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <returns>The advisory lock key.</returns>
    public static long Key(CompetitionId competitionId)
    {
        var bytes = competitionId.Value.ToByteArray();
        return BitConverter.ToInt64(bytes, 0) ^ BitConverter.ToInt64(bytes, 8);
    }
}
