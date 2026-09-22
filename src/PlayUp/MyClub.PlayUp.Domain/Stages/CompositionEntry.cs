// -----------------------------------------------------------------------
// <copyright file="CompositionEntry.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// One entry in the stage runtime population membership (<see cref="Stage.CompositionEntries"/>).
/// Distinct from Affectation authoring, <see cref="DirectAssignment"/> (slot feed), and slot/group resolution.
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
