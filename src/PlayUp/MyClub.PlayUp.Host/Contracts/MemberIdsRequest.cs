// -----------------------------------------------------------------------
// <copyright file="MemberIdsRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for a named declared-member lot command.
/// </summary>
/// <param name="MemberIds">Declared member identities in the lot.</param>
public sealed record MemberIdsRequest(IReadOnlyList<Guid> MemberIds);
