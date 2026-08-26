// -----------------------------------------------------------------------
// <copyright file="GenerateNextRoundResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Result of <see cref="GenerateNextRound"/>.
/// </summary>
/// <param name="RoundIndex">Swiss round / Matchday number generated or already present.</param>
/// <param name="CreatedMatches">Newly created matches (empty when already complete).</param>
/// <param name="ByeEntryId">Bye recipient when N odd; otherwise null.</param>
/// <param name="AlreadyComplete">True when the round was already fully materialized (idempotent).</param>
public sealed record GenerateNextRoundResult(
    int RoundIndex,
    IReadOnlyList<Match> CreatedMatches,
    EntryId? ByeEntryId,
    bool AlreadyComplete);
