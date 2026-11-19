// -----------------------------------------------------------------------
// <copyright file="GroupPlacementRejectedException.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application;

/// <summary>
/// A group lot refused one entry. The HTTP problem carries <see cref="Code"/> and <see cref="EntryId"/>.
/// </summary>
/// <param name="message">Error message.</param>
/// <param name="code">Stable machine-readable code.</param>
/// <param name="entryId">The entry the lot refused.</param>
public sealed class GroupPlacementRejectedException(string message, string code, EntryId entryId)
    : Exception(message)
{
    /// <summary>
    /// Gets the stable failure code.
    /// </summary>
    public string Code { get; } = code;

    /// <summary>
    /// Gets the entry the lot refused.
    /// </summary>
    public EntryId EntryId { get; } = entryId;
}
