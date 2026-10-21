// -----------------------------------------------------------------------
// <copyright file="DirectFeedSource.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Snapshot entry for a direct assignment feed (configuration — not <see cref="Slot.EntryId"/>).
/// </summary>
public sealed record DirectFeedSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DirectFeedSource"/> class.
    /// </summary>
    /// <param name="slotKey">Target slot key.</param>
    /// <param name="configuredEntryId"><see cref="DirectAssignment.EntryId"/>.</param>
    public DirectFeedSource(string slotKey, EntryId configuredEntryId)
    {
        SlotKey = Slot.NormalizeKey(slotKey);
        ConfiguredEntryId = configuredEntryId;
    }

    /// <summary>
    /// Gets the target slot key.
    /// </summary>
    public string SlotKey { get; }

    /// <summary>
    /// Gets the configured entry from the direct assignment.
    /// </summary>
    public EntryId ConfiguredEntryId { get; }
}
