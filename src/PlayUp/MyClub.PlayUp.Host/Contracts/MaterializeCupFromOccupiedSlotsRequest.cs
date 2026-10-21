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
/// Optional explicit <c>BracketPair</c> keys (e.g. P1, P3).
/// Omit / null / empty → all eligible pairs on the stage.
/// </param>
public sealed record MaterializeCupFromOccupiedSlotsRequest(
    IReadOnlyList<string>? PairKeys = null);
