// -----------------------------------------------------------------------
// <copyright file="DrawTestPoolSync.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Tests.Common;

/// <summary>
/// Aligns stage population with draw pool in tests when neither affectation nor composition was seeded.
/// </summary>
internal static class DrawTestPoolSync
{
    /// <summary>
    /// Ensures the effective entry pool covers <paramref name="pool"/> without duplicating inbound-only authoring.
    /// </summary>
    public static void EnsureEffectivePool(Stage stage, IReadOnlyList<EntryId> pool, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentNullException.ThrowIfNull(clock);

        if (pool.Count == 0)
        {
            return;
        }

        var composition = stage.CompositionEntries.Select(entry => entry.EntryId).ToHashSet();
        var affectation = stage.AffectationAuthoring.Select(entry => entry.EntryId).ToHashSet();

        if (affectation.Count == 0 && pool.All(composition.Contains))
        {
            return;
        }

        if (pool.All(entryId => affectation.Contains(entryId) && composition.Contains(entryId)))
        {
            return;
        }

        stage.ReplaceAffectationAuthoring([.. pool], clock);
    }
}
