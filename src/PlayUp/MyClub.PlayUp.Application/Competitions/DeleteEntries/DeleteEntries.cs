// -----------------------------------------------------------------------
// <copyright file="DeleteEntries.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Named atomic batch: hard-delete several entries. One gate failure refuses the whole lot.
/// </summary>
public static class DeleteEntries
{
    /// <summary>
    /// Deletes every listed entry after all gates succeed.
    /// </summary>
    public static void Execute(
        Competition competition,
        IReadOnlyList<EntryId> entryIds,
        IReadOnlyList<Match> competitionMatches,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(entryIds);
        ArgumentNullException.ThrowIfNull(competitionMatches);
        ArgumentNullException.ThrowIfNull(clock);
        EntryBatch.EnsureNonEmptyDistinct(entryIds);

        foreach (var entryId in entryIds)
        {
            _ = competition.GetEntry(entryId);
            DeleteEntry.EnsureNoSportingHistory(entryId, competitionMatches);
        }

        foreach (var entryId in entryIds)
        {
            competition.DeleteEntry(entryId, clock);
        }
    }
}
