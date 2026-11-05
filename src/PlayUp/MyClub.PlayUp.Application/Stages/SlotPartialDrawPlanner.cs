// -----------------------------------------------------------------------
// <copyright file="SlotPartialDrawPlanner.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application adapter for <see cref="SlotPartialDraw"/>: feed snapshot, refusal codes, pool match.
/// </summary>
public static class SlotPartialDrawPlanner
{
    /// <summary>
    /// Composes the partial slot scope for <paramref name="stage"/>.
    /// </summary>
    /// <param name="stage">Phase being drawn.</param>
    /// <param name="competitionStages">Competition stages used to resolve inbound feeds.</param>
    /// <returns>The domain scope.</returns>
    public static SlotPartialDrawScope Compose(Stage stage, IReadOnlyList<Stage> competitionStages)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);

        var snapshot = SlotFeedSnapshotAssembler.Assemble(stage, competitionStages);
        return SlotPartialDraw.Compose(stage, snapshot);
    }

    /// <summary>
    /// Throws when <paramref name="scope"/> cannot be drawn.
    /// </summary>
    /// <param name="scope">Composed partial scope.</param>
    public static void EnsureDrawable(SlotPartialDrawScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (scope.IsDrawable)
        {
            return;
        }

        var (code, message) = scope.Refusal switch
        {
            SlotPartialDrawRefusal.NoFreeSlot => (
                ApplicationErrorCodes.DrawNoFreeSlot,
                "Partial draw requires at least one free slot. Existing occupants stay in place."),
            SlotPartialDrawRefusal.NotEnoughTeams => (
                ApplicationErrorCodes.DrawNotEnoughTeams,
                "Partial draw requires enough unplaced teams to fill every free slot."),
            SlotPartialDrawRefusal.TooManyTeams => (
                ApplicationErrorCodes.DrawTooManyTeams,
                "Partial draw cannot place more unplaced teams than there are free slots."),
            _ => throw new InvalidOperationException($"Unexpected partial draw refusal '{scope.Refusal}'."),
        };

        throw new ApplicationFailureException(message, code);
    }

    /// <summary>
    /// Returns whether <paramref name="entries"/> is the current unplaced pool, ignoring order.
    /// </summary>
    /// <param name="entries">Draw input pool.</param>
    /// <param name="unplaced">Unplaced composition entries.</param>
    /// <returns><see langword="true"/> when the two sets are equal.</returns>
    public static bool EntriesMatch(IReadOnlyList<EntryId> entries, IReadOnlyList<EntryId> unplaced)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(unplaced);
        return entries.Count == unplaced.Count && entries.ToHashSet().SetEquals(unplaced);
    }
}
