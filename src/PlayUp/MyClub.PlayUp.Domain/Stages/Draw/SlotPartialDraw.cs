// -----------------------------------------------------------------------
// <copyright file="SlotPartialDraw.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Decides which slot-draw places stay untouched and which teams may be drawn into the rest.
/// </summary>
/// <remarks>
/// A free slot has no occupant and a missing feed. Occupants (manual or a previous resolution)
/// and reserved or invalid feeds stay outside the draw. The draw is a bijection of the unplaced
/// teams onto those free slots — existing occupants are not targets and are not in the pool.
/// </remarks>
public static class SlotPartialDraw
{
    /// <summary>
    /// Composes the partial slot-draw scope from the stage and its feed snapshot.
    /// </summary>
    /// <param name="stage">Phase whose slots and composition are read.</param>
    /// <param name="snapshot">Feed snapshot for <paramref name="stage"/> (same slot keys).</param>
    /// <returns>Vacant slots, unplaced teams, and the refusal when they do not match.</returns>
    public static SlotPartialDrawScope Compose(Stage stage, SlotFeedSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(snapshot);

        var occupants = new HashSet<EntryId>();
        var vacant = new List<string>();

        foreach (var slot in stage.Slots)
        {
            if (slot.EntryId is { } occupant)
            {
                occupants.Add(occupant);
                continue;
            }

            var resolution = SlotFeedResolver.Resolve(snapshot, slot.SlotKey);
            if (resolution.Status == FeedResolutionStatus.Missing)
            {
                vacant.Add(slot.SlotKey);
            }
        }

        var unplaced = new List<EntryId>();
        foreach (var entry in stage.CompositionEntries)
        {
            if (!occupants.Contains(entry.EntryId))
            {
                unplaced.Add(entry.EntryId);
            }
        }

        var refusal = vacant.Count == 0
            ? SlotPartialDrawRefusal.NoFreeSlot
            : unplaced.Count < vacant.Count
                ? SlotPartialDrawRefusal.NotEnoughTeams
                : unplaced.Count > vacant.Count
                    ? SlotPartialDrawRefusal.TooManyTeams
                    : SlotPartialDrawRefusal.None;

        return new SlotPartialDrawScope(vacant, unplaced, refusal);
    }
}

/// <summary>
/// Why a slot phase cannot host a partial draw of its free places.
/// </summary>
public enum SlotPartialDrawRefusal
{
    /// <summary>The free places and the unplaced teams form a bijection.</summary>
    None = 0,

    /// <summary>No slot is free for a draw.</summary>
    NoFreeSlot = 1,

    /// <summary>Fewer unplaced teams than free slots.</summary>
    NotEnoughTeams = 2,

    /// <summary>More unplaced teams than free slots.</summary>
    TooManyTeams = 3,
}

/// <summary>
/// Free slots and unplaced teams for one partial slot draw.
/// </summary>
/// <param name="VacantSlotKeys">Slots with no occupant and no structural feed.</param>
/// <param name="UnplacedEntries">Composition entries that do not occupy a slot.</param>
/// <param name="Refusal">Why the scope cannot be drawn; <see cref="SlotPartialDrawRefusal.None"/> when it can.</param>
public sealed record SlotPartialDrawScope(
    IReadOnlyList<string> VacantSlotKeys,
    IReadOnlyList<EntryId> UnplacedEntries,
    SlotPartialDrawRefusal Refusal)
{
    /// <summary>
    /// Gets a value indicating whether unplaced teams fill every free slot exactly.
    /// </summary>
    public bool IsDrawable => Refusal == SlotPartialDrawRefusal.None;
}
