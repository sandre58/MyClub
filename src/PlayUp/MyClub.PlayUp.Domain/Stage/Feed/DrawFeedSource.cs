// -----------------------------------------------------------------------
// <copyright file="DrawFeedSource.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Snapshot placeholder for a draw feed target (Draw Entity not required in V1).
/// </summary>
public sealed record DrawFeedSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DrawFeedSource"/> class.
    /// </summary>
    /// <param name="slotKey">Target slot key.</param>
    public DrawFeedSource(string slotKey) => SlotKey = Slot.NormalizeKey(slotKey);

    /// <summary>
    /// Gets the target slot key.
    /// </summary>
    public string SlotKey { get; }
}
