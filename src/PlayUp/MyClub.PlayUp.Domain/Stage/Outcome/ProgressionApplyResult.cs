// -----------------------------------------------------------------------
// <copyright file="ProgressionApplyResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Deterministic occupant instruction produced by <see cref="ProgressionApplier"/> (no Stage mutation).
/// </summary>
public sealed record ProgressionApplyResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressionApplyResult"/> class.
    /// </summary>
    /// <param name="destinationStageId">Stage that owns the destination slot.</param>
    /// <param name="slotKey">Opaque destination slot key.</param>
    /// <param name="entryId">Entry selected from the fixture outcome (winner or loser).</param>
    public ProgressionApplyResult(StageId destinationStageId, string slotKey, EntryId entryId)
    {
        ArgumentNullException.ThrowIfNull(slotKey);

        DestinationStageId = destinationStageId;
        SlotKey = slotKey;
        EntryId = entryId;
    }

    /// <summary>
    /// Gets the destination stage identity.
    /// </summary>
    public StageId DestinationStageId { get; }

    /// <summary>
    /// Gets the destination slot key.
    /// </summary>
    public string SlotKey { get; }

    /// <summary>
    /// Gets the entry to place in the destination slot.
    /// </summary>
    public EntryId EntryId { get; }
}
