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
/// A free slot has no occupant and a missing feed. Composition occupants (manual or a previous
/// resolution) and reserved or invalid feeds stay outside the draw. An occupant that is no longer
/// in <see cref="Stage.CompositionEntries"/> is withdrawn: not free, and not an unplaced team.
/// The draw is a bijection of the unplaced teams onto the free slots.
/// </remarks>
public static class SlotPartialDraw
{
    /// <summary>
    /// Classifies each slot for partial-draw counting and for the draft cup display.
    /// </summary>
    /// <param name="stage">Phase whose slots and composition are read.</param>
    /// <param name="snapshot">Feed snapshot for <paramref name="stage"/> (same slot keys).</param>
    /// <returns>One fact per slot, in stage order.</returns>
    public static IReadOnlyList<SlotDrawPlaceFact> Classify(Stage stage, SlotFeedSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(snapshot);

        var composition = stage.CompositionEntries.Select(entry => entry.EntryId).ToHashSet();
        var facts = new List<SlotDrawPlaceFact>(stage.Slots.Count);
        foreach (var slot in stage.Slots)
        {
            var feed = SlotFeedResolver.Resolve(snapshot, slot.SlotKey);
            facts.Add(new SlotDrawPlaceFact(
                slot.SlotKey,
                ClassifyOccupancy(slot.EntryId, composition, feed),
                slot.EntryId));
        }

        return facts;
    }

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

        var composition = stage.CompositionEntries.Select(entry => entry.EntryId).ToHashSet();
        var facts = Classify(stage, snapshot);

        // Withdrawn occupants are outside composition: they must not shrink the unplaced pool
        // and their slot is not free. Only composition occupants count as placed.
        var placed = new HashSet<EntryId>();
        var vacant = new List<string>();
        foreach (var fact in facts)
        {
            if (fact.Occupancy == SlotDrawPlaceOccupancy.Free)
            {
                vacant.Add(fact.SlotKey);
            }

            if (fact.EntryId is { } occupant && composition.Contains(occupant))
            {
                placed.Add(occupant);
            }
        }

        var unplaced = new List<EntryId>();
        foreach (var entry in stage.CompositionEntries)
        {
            if (!placed.Contains(entry.EntryId))
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

    /// <summary>
    /// A unique direct assignment is a manual placement. Qualification, progression, draw-target,
    /// and non-unique feeds reserve the slot (not drawable, not withdrawn).
    /// </summary>
    private static bool IsStructuralFeed(SlotFeedResolution feed) =>
        feed.Status != FeedResolutionStatus.Missing
        && !(feed.Status == FeedResolutionStatus.Unique && feed.Source?.Kind == FeedKind.Direct);

    private static SlotDrawPlaceOccupancy ClassifyOccupancy(
        EntryId? occupant,
        HashSet<EntryId> composition,
        SlotFeedResolution feed)
    {
        if (IsStructuralFeed(feed))
        {
            return SlotDrawPlaceOccupancy.StructuralFeed;
        }

        if (occupant is not { } entryId)
        {
            return feed.Status == FeedResolutionStatus.Missing
                ? SlotDrawPlaceOccupancy.Free
                : SlotDrawPlaceOccupancy.StructuralFeed;
        }

        return composition.Contains(entryId)
            ? SlotDrawPlaceOccupancy.AlreadyPlaced
            : SlotDrawPlaceOccupancy.Withdrawn;
    }
}

/// <summary>
/// How a slot counts for a partial draw and for the draft cup grid.
/// </summary>
public enum SlotDrawPlaceOccupancy
{
    /// <summary>No occupant and a missing feed. Counts as vacant.</summary>
    Free = 0,

    /// <summary>Composition occupant without a structural feed. Not vacant, not unplaced.</summary>
    AlreadyPlaced = 1,

    /// <summary>Structural feed (or a direct feed with no occupant). Not vacant.</summary>
    StructuralFeed = 2,

    /// <summary>Occupant outside composition, without a structural feed. Not vacant, not unplaced.</summary>
    Withdrawn = 3,
}

/// <summary>
/// Occupancy of one slot under <see cref="SlotPartialDraw.Classify"/>.
/// </summary>
/// <param name="SlotKey">Slot key.</param>
/// <param name="Occupancy">Counting class. Withdrawn is neither vacant nor unplaced.</param>
/// <param name="EntryId">Current occupant when the slot has one. Not a feed.</param>
public sealed record SlotDrawPlaceFact(
    string SlotKey,
    SlotDrawPlaceOccupancy Occupancy,
    EntryId? EntryId);

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
