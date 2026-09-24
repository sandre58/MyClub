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
/// <param name="Paths">Replacement paths (empty or null clears rules).</param>
public sealed record ReplaceStagePlacementAwardRulesRequest(
    IReadOnlyList<PlacementAwardPathRequest>? Paths);

/// <summary>
/// One HTTP placement award path.
/// Prefer <see cref="SourcePairKey"/>; legacy <see cref="SourceFixtureId"/> dual-read at Host for cutover.
/// </summary>
/// <param name="Outcome">Winner or Loser.</param>
/// <param name="Rank">1-based final competition rank awarded.</param>
/// <param name="SourcePairKey">Structural source key (Cup = PairKey).</param>
/// <param name="SourceFixtureId">Legacy dual-read only — never Path identity.</param>
public sealed record PlacementAwardPathRequest(
    ProgressionOutcome Outcome,
    int Rank,
    string? SourcePairKey = null,
    Guid? SourceFixtureId = null);
