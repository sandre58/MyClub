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
/// <param name="Paths">
/// Award paths. A non-empty list replaces rules. Absent, null, or empty is accepted only when <paramref name="Clear"/> is <see langword="true"/>.
/// </param>
/// <param name="Clear">
/// When <see langword="true"/>, clears rules. Mutually exclusive with a non-empty <paramref name="Paths"/> list.
/// </param>
public sealed record ReplaceStagePlacementAwardRulesRequest(
    IReadOnlyList<PlacementAwardPathRequest>? Paths = null,
    bool Clear = false);

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
