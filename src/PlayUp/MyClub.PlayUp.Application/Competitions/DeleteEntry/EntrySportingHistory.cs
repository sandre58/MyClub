// -----------------------------------------------------------------------
// <copyright file="EntrySportingHistory.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Detects sporting history that blocks hard-delete of a competition entry.
/// A match that lists the entry as home or away is enough (sheet / goals / cards / subs live on that match).
/// </summary>
internal static class EntrySportingHistory
{
    public static bool Exists(EntryId entryId, IReadOnlyList<Match> competitionMatches)
    {
        ArgumentNullException.ThrowIfNull(competitionMatches);
        return competitionMatches.Any(match =>
            match.HomeEntryId.Equals(entryId) || match.AwayEntryId.Equals(entryId));
    }
}
