// -----------------------------------------------------------------------
// <copyright file="GenerateNextRoundResponse.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for <c>POST /stages/{stageId}/swiss/generate-next-round</c>.
/// </summary>
/// <param name="RoundIndex">Swiss round / Matchday number generated or already present.</param>
/// <param name="CreatedCount">Number of Match aggregates created in this call.</param>
/// <param name="AttachedMatchIds">Match identities created (empty when already complete).</param>
/// <param name="ByeEntryId">Bye recipient when N odd; otherwise null.</param>
/// <param name="AlreadyComplete">True when the round was already fully materialized (idempotent).</param>
public sealed record GenerateNextRoundResponse(
    int RoundIndex,
    int CreatedCount,
    IReadOnlyList<Guid> AttachedMatchIds,
    Guid? ByeEntryId,
    bool AlreadyComplete);
