// -----------------------------------------------------------------------
// <copyright file="SlotAssignmentInstruction.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Deterministic occupant instruction for a slot (Qualification, Progression, Draw Slot).
/// Not persisted — truth remains <see cref="Slot.EntryId"/> after <see cref="Stage.ApplyResolvedEntry"/>.
/// </summary>
public sealed record SlotAssignmentInstruction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlotAssignmentInstruction"/> class.
    /// </summary>
    /// <param name="stageId">Stage that owns the destination slot.</param>
    /// <param name="slotKey">Opaque destination slot key.</param>
    /// <param name="entryId">Entry to place in the destination slot.</param>
    public SlotAssignmentInstruction(StageId stageId, string slotKey, EntryId entryId)
    {
        ArgumentNullException.ThrowIfNull(slotKey);

        StageId = stageId;
        SlotKey = slotKey;
        EntryId = entryId;
    }

    /// <summary>
    /// Gets the destination stage identity.
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the destination slot key.
    /// </summary>
    public string SlotKey { get; }

    /// <summary>
    /// Gets the entry to place in the destination slot.
    /// </summary>
    public EntryId EntryId { get; }
}
