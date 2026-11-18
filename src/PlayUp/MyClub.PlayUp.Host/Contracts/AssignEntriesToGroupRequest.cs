// -----------------------------------------------------------------------
// <copyright file="AssignEntriesToGroupRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for placing several entries in one group.
/// </summary>
/// <param name="EntryIds">Entries in the order they should be appended. At most 64, no duplicates.</param>
public sealed record AssignEntriesToGroupRequest(IReadOnlyList<Guid>? EntryIds);
