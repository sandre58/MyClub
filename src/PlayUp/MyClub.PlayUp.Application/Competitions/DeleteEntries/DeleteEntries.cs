// -----------------------------------------------------------------------
// <copyright file="DeleteEntries.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Named atomic batch: hard-delete several entries. One gate failure refuses the whole lot.
/// </summary>
public static class DeleteEntries
{
    /// <summary>
    /// Deletes every listed entry after all exist (cascade-deletes referencing matches).
    /// </summary>
    public static void Execute(
        Competition competition,
        IReadOnlyList<EntryId> entryIds,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyList<Match> competitionMatches,
        IMatchRepository matchRepository,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(entryIds);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(competitionMatches);
        ArgumentNullException.ThrowIfNull(matchRepository);
        ArgumentNullException.ThrowIfNull(clock);
        EntryBatch.EnsureNonEmptyDistinct(entryIds);

        foreach (var entryId in entryIds)
        {
            _ = competition.GetEntry(entryId);
        }

        var remainingMatches = competitionMatches.ToList();
        foreach (var entryId in entryIds)
        {
            DeleteEntry.RemoveMatchesForEntry(
                entryId,
                competitionStages,
                remainingMatches,
                matchRepository,
                clock);
            remainingMatches.RemoveAll(match =>
                match.HomeEntryId.Equals(entryId) || match.AwayEntryId.Equals(entryId));
            competition.DeleteEntry(entryId, clock);
        }
    }
}
