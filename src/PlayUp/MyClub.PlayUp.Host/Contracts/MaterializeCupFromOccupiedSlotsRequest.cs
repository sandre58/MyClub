// -----------------------------------------------------------------------
// <copyright file="MaterializeCupFromOccupiedSlotsRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for materializing Cup Fixtures/Matches from occupied bracket pairs.
/// </summary>
/// <param name="PairKeys">
/// Explicit <c>BracketPair</c> keys (e.g. P1, P3). Non-empty plans only those pairs.
/// </param>
/// <param name="PlanEntireRound">
/// When true and <paramref name="PairKeys"/> is omitted or empty, plans every eligible pair on the round.
/// Omit or false without keys is refused.
/// </param>
public sealed record MaterializeCupFromOccupiedSlotsRequest(
    IReadOnlyList<string>? PairKeys = null,
    bool PlanEntireRound = false);
