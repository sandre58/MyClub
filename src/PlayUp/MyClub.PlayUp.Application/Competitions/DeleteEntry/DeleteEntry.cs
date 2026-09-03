// -----------------------------------------------------------------------
// <copyright file="DeleteEntry.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: hard-delete an entry during construction when it has no sporting history.
/// </summary>
/// <remarks>
/// Domain owns Draft/Ready. Application owns the match/nominative-history gate (matches are another aggregate).
/// Structure holes do not block delete. After Start, Domain refuses — use <see cref="WithdrawEntry"/>.
/// </remarks>
public static class DeleteEntry
{
    /// <summary>
    /// Deletes the entry when no competition match references it.
    /// </summary>
    public static void Execute(
        Competition competition,
        EntryId entryId,
        IReadOnlyList<Match> competitionMatches,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(competitionMatches);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureNoSportingHistory(entryId, competitionMatches);
        competition.DeleteEntry(entryId, clock);
    }

    internal static void EnsureNoSportingHistory(EntryId entryId, IReadOnlyList<Match> competitionMatches)
    {
        if (EntrySportingHistory.Exists(entryId, competitionMatches))
        {
            throw new ApplicationFailureException(
                $"Entry '{entryId}' cannot be deleted because it is referenced by a match.",
                ApplicationErrorCodes.EntryHasSportingHistory);
        }
    }
}
