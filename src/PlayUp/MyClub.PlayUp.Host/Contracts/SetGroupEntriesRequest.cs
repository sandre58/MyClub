// -----------------------------------------------------------------------
// <copyright file="SetGroupEntriesRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// Body for atomically setting a group's membership.
/// </summary>
/// <param name="EntryIds">The target membership. Order controls append order for new members.</param>
/// <param name="ExpectedEntryIds">
/// Optimistic concurrency: must match the current membership set on a mutation. May be omitted on a set noop.
/// </param>
public sealed record SetGroupEntriesRequest(
    IReadOnlyList<Guid>? EntryIds,
    IReadOnlyList<Guid>? ExpectedEntryIds);
