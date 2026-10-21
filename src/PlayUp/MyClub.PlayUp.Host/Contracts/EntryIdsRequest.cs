// -----------------------------------------------------------------------
// <copyright file="EntryIdsRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for a named entry-lot command.
/// </summary>
/// <param name="EntryIds">Entry identities in the lot.</param>
public sealed record EntryIdsRequest(IReadOnlyList<Guid> EntryIds);
