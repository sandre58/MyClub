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
/// </summary>
/// <param name="SourceFixtureId">Fixture on the rules-owning stage.</param>
/// <param name="Outcome">Winner or Loser.</param>
/// <param name="Rank">1-based final competition rank awarded.</param>
public sealed record PlacementAwardPathRequest(
    Guid SourceFixtureId,
    ProgressionOutcome Outcome,
    int Rank);
