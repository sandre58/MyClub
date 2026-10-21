// -----------------------------------------------------------------------
// <copyright file="EntryBatch.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Shared guards for named entry-lot commands.
/// </summary>
internal static class EntryBatch
{
    public static void EnsureNonEmptyDistinct<T>(IReadOnlyList<T> ids)
        where T : struct, IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
        {
            throw new ApplicationFailureException(
                "A named entry lot requires at least one identity.",
                ApplicationErrorCodes.EmptyEntryLot);
        }

        if (ids.Distinct().Count() != ids.Count)
        {
            throw new ApplicationFailureException(
                "A named entry lot cannot contain duplicate identities.",
                ApplicationErrorCodes.DuplicateEntryLotId);
        }
    }
}
