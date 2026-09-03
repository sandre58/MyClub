// -----------------------------------------------------------------------
// <copyright file="WithdrawEntries.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Named atomic batch: withdraw several entries. One gate failure refuses the whole lot.
/// </summary>
public static class WithdrawEntries
{
    /// <summary>
    /// Withdraws every listed entry after all exist and are Active.
    /// </summary>
    public static void Execute(
        Competition competition,
        IReadOnlyList<EntryId> entryIds,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(entryIds);
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
            competition.WithdrawEntry(entryId, clock);
        }
    }
}
