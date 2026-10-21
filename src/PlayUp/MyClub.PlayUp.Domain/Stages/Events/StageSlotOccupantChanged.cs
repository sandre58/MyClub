// -----------------------------------------------------------------------
// <copyright file="StageSlotOccupantChanged.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages.Events;

/// <summary>
/// Raised when a slot's resolved occupant changes via dynamic resolution (not DirectAssignment).
/// </summary>
public sealed record StageSlotOccupantChanged : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageSlotOccupantChanged"/> class.
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="slotKey">The target slot key whose occupant changed.</param>
    /// <param name="previousEntryId">The previous occupant of the target slot, or <see langword="null"/> if vacant.</param>
    /// <param name="entryId">The new occupant of the target slot, or <see langword="null"/> if cleared.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public StageSlotOccupantChanged(
        StageId stageId,
        string slotKey,
        EntryId? previousEntryId,
        EntryId? entryId,
        IClock clock)
        : base(clock)
    {
        StageId = stageId;
        SlotKey = slotKey;
        PreviousEntryId = previousEntryId;
        EntryId = entryId;
    }

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the target slot key.
    /// </summary>
    public string SlotKey { get; }

    /// <summary>
    /// Gets the previous occupant of the target slot, or <see langword="null"/> if it was vacant.
    /// </summary>
    public EntryId? PreviousEntryId { get; }

    /// <summary>
    /// Gets the new occupant of the target slot, or <see langword="null"/> if cleared.
    /// </summary>
    public EntryId? EntryId { get; }
}
