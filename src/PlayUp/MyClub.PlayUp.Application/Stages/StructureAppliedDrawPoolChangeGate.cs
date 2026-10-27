// -----------------------------------------------------------------------
// <copyright file="StructureAppliedDrawPoolChangeGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Refuses affectation pool <see cref="EntryId"/> set changes when a non-cancelled draw is Applied on the stage.
/// </summary>
internal static class StructureAppliedDrawPoolChangeGate
{
    internal static void EnsurePoolChangeAllowed(Stage stage, IReadOnlyList<EntryId> proposedEntryIds)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(proposedEntryIds);

        var current = stage.AffectationAuthoring.Select(entry => entry.EntryId).ToHashSet();
        var proposed = proposedEntryIds.ToHashSet();
        if (current.SetEquals(proposed))
        {
            return;
        }

        if (stage.Draws.Any(draw => draw.Status != DrawStatus.Cancelled && DrawAppliedState.IsApplied(draw, stage)))
        {
            throw new ApplicationFailureException(
                "Affectation pool cannot change while an applied draw is on this stage.",
                ApplicationErrorCodes.AppliedDrawPoolChangeBlocked);
        }
    }
}
