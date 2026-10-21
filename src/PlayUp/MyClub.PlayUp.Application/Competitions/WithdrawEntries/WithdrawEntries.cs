// -----------------------------------------------------------------------
// <copyright file="WithdrawEntries.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Named atomic batch: forfait several entries. One gate failure refuses the whole lot.
/// </summary>
public static class WithdrawEntries
{
    /// <summary>
    /// Withdraws every listed entry after all exist and are Active (forfeit remaining matches first).
    /// </summary>
    public static void Execute(
        Competition competition,
        IReadOnlyList<EntryId> entryIds,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyList<Match> competitionMatches,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(entryIds);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(competitionMatches);
        ArgumentNullException.ThrowIfNull(clock);
        EntryBatch.EnsureNonEmptyDistinct(entryIds);

        foreach (var entryId in entryIds)
        {
            var entry = competition.GetEntry(entryId);
            if (entry.Status != EntryStatus.Active)
            {
                throw new ApplicationFailureException(
                    $"Entry '{entryId}' cannot be withdrawn (status '{entry.Status}').",
                    ApplicationErrorCodes.EntryNotWithdrawable);
            }
        }

        foreach (var entryId in entryIds)
        {
            WithdrawEntry.FinishRemainingMatchesAsForfeit(
                entryId,
                competitionStages,
                competitionMatches,
                clock);
            competition.WithdrawEntry(entryId, clock);
        }
    }
}
