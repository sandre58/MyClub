// -----------------------------------------------------------------------
// <copyright file="PathOccupantSelectionResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Qualification;

/// <summary>
/// Outcome status for a pure path occupant selection.
/// </summary>
public enum PathOccupantSelectionStatus
{
    /// <summary>
    /// Exactly one entry was selected.
    /// </summary>
    Resolved = 1,

    /// <summary>
    /// Selection did not resolve to exactly one entry.
    /// </summary>
    Indeterminate = 2,

    /// <summary>
    /// Qualification condition gate skipped the path.
    /// </summary>
    Skipped = 3,
}

/// <summary>
/// Pure selection outcome for a qualification or progression path (no exceptions for unresolved cases).
/// </summary>
public sealed record PathOccupantSelectionResult(PathOccupantSelectionStatus Status, EntryId? EntryId = null)
{
    /// <summary>
    /// Creates a resolved selection.
    /// </summary>
    public static PathOccupantSelectionResult Resolved(EntryId entryId) =>
        new(PathOccupantSelectionStatus.Resolved, entryId);

    /// <summary>
    /// Creates an indeterminate selection.
    /// </summary>
    public static PathOccupantSelectionResult Indeterminate() =>
        new(PathOccupantSelectionStatus.Indeterminate);

    /// <summary>
    /// Creates a skipped selection.
    /// </summary>
    public static PathOccupantSelectionResult Skipped() =>
        new(PathOccupantSelectionStatus.Skipped);
}
