// -----------------------------------------------------------------------
// <copyright file="CompositionEntry.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Entry identity row shared by two distinct Stage collections:
/// <see cref="Stage.AffectationAuthoring"/> (manual producers) and
/// <see cref="Stage.CompositionEntries"/> (runtime population / Draw·Live pool).
/// Same shape, different semantics — do not treat the lists as interchangeable.
/// Also distinct from <see cref="DirectAssignment"/> (slot feed) and group/slot resolution.
/// </summary>
public sealed record CompositionEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompositionEntry"/> class.
    /// </summary>
    /// <param name="entryId">Competition entry identity.</param>
    public CompositionEntry(EntryId entryId) => EntryId = entryId;

    /// <summary>
    /// Gets the competition entry identity.
    /// </summary>
    public EntryId EntryId { get; }
}
