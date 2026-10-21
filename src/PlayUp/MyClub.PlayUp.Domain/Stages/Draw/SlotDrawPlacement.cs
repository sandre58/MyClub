// -----------------------------------------------------------------------
// <copyright file="SlotDrawPlacement.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Entry → Slot placement used as a fixed Draw input and/or Slot resolution result.
/// Not a DirectAssignment and not a generic Assignment abstraction.
/// </summary>
public sealed record SlotDrawPlacement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlotDrawPlacement"/> class.
    /// </summary>
    /// <param name="entryId">The entry identity.</param>
    /// <param name="slotKey">Destination slot key.</param>
    public SlotDrawPlacement(EntryId entryId, string slotKey)
    {
        EntryId = entryId;
        SlotKey = Slot.NormalizeKey(slotKey);
    }

    /// <summary>
    /// Gets the entry identity.
    /// </summary>
    public EntryId EntryId { get; }

    /// <summary>
    /// Gets the destination slot key.
    /// </summary>
    public string SlotKey { get; }
}
