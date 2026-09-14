// -----------------------------------------------------------------------
// <copyright file="ReplaceStageCompositionEntries.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: replace the root composition entry set on a stage.
/// </summary>
public static class ReplaceStageCompositionEntries
{
    /// <summary>
    /// Replaces composition entries. Each identity must belong to the competition.
    /// Partial sets are allowed (Draft-persistable).
    /// </summary>
    public static void Execute(
        Stage stage,
        Competition competition,
        IReadOnlyList<EntryId> entryIds,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(entryIds);
        ArgumentNullException.ThrowIfNull(clock);

        foreach (var entryId in entryIds)
        {
            _ = competition.GetEntry(entryId);
        }

        stage.ReplaceCompositionEntries(entryIds, clock);
    }
}
