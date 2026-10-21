// -----------------------------------------------------------------------
// <copyright file="MaterializeMatchesResponse.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for <c>POST /stages/{stageId}/matches/materialize</c>.
/// </summary>
/// <param name="CreatedCount">Number of Match aggregates created in this call.</param>
/// <param name="AttachedMatchIds">All match identities attached after the operation.</param>
/// <param name="AlreadyComplete">True when expected matches were already present (idempotent).</param>
public sealed record MaterializeMatchesResponse(
    int CreatedCount,
    IReadOnlyList<Guid> AttachedMatchIds,
    bool AlreadyComplete);
