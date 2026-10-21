// -----------------------------------------------------------------------
// <copyright file="DirectAssignment.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Explicit configuration feed assigning an entry to a slot (distinct from <see cref="Slot.EntryId"/> resolution).
/// </summary>
public sealed record DirectAssignment
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DirectAssignment"/> class.
    /// </summary>
    /// <param name="slotKey">Target slot key.</param>
    /// <param name="entryId">Configured entry identity.</param>
    public DirectAssignment(string slotKey, EntryId entryId)
    {
        SlotKey = Slot.NormalizeKey(slotKey);
        EntryId = entryId;
    }

    /// <summary>
    /// Gets the target slot key.
    /// </summary>
    public string SlotKey { get; }

    /// <summary>
    /// Gets the configured entry identity.
    /// </summary>
    public EntryId EntryId { get; }
}
