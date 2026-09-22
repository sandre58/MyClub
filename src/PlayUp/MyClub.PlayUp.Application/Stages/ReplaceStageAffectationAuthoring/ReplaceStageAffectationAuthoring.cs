// -----------------------------------------------------------------------
// <copyright file="ReplaceStageAffectationAuthoring.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: replace stage Affectation authoring (manual population producers).
/// Syncs runtime <see cref="Stage.CompositionEntries"/> by diff — does not wipe Apply resolutions.
/// </summary>
public static class ReplaceStageAffectationAuthoring
{
    /// <summary>
    /// Replaces Affectation authoring. Each identity must belong to the competition.
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

        stage.ReplaceAffectationAuthoring(entryIds, clock);
    }
}
