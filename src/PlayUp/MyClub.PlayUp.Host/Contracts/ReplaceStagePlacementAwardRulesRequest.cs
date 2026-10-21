// -----------------------------------------------------------------------
// <copyright file="ReplaceStagePlacementAwardRulesRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for replacing placement award rules on a stage.
/// </summary>
/// <param name="Paths">Award paths (empty or null clears).</param>
public sealed record ReplaceStagePlacementAwardRulesRequest(
    IReadOnlyList<PlacementAwardPathRequest>? Paths = null);

/// <summary>
/// One HTTP placement award path. Structural identity = <see cref="SourcePairKey"/> (Cup = PairKey).
/// </summary>
/// <param name="Outcome">Winner or Loser.</param>
/// <param name="Rank">1-based final competition rank.</param>
/// <param name="SourcePairKey">Structural source key (Cup = PairKey). Required.</param>
public sealed record PlacementAwardPathRequest(
    ProgressionOutcome Outcome,
    int Rank,
    string SourcePairKey);
