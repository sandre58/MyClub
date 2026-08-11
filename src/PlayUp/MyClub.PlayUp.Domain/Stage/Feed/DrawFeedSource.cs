// -----------------------------------------------------------------------
// <copyright file="DrawFeedSource.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Snapshot row: a Slot is fed by a published Draw Slot resolution
/// (Draw → SlotResolution → DrawFeedSource → WhoFeeds).
/// </summary>
public sealed record DrawFeedSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DrawFeedSource"/> class.
    /// </summary>
    /// <param name="slotKey">Target slot key.</param>
    /// <param name="drawId">Published draw identity.</param>
    public DrawFeedSource(string slotKey, DrawId drawId)
    {
        SlotKey = Slot.NormalizeKey(slotKey);
        DrawId = drawId;
    }

    /// <summary>
    /// Gets the target slot key.
    /// </summary>
    public string SlotKey { get; }

    /// <summary>
    /// Gets the published draw identity.
    /// </summary>
    public DrawId DrawId { get; }
}
