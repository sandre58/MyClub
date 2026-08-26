// -----------------------------------------------------------------------
// <copyright file="ReplaceStageProgressionRulesRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for replacing progression rules on a stage.
/// </summary>
/// <param name="Paths">Replacement paths (empty or null clears rules).</param>
public sealed record ReplaceStageProgressionRulesRequest(
    IReadOnlyList<ProgressionPathRequest>? Paths);

/// <summary>
/// One HTTP progression path.
/// </summary>
/// <param name="SourceFixtureId">Fixture on the rules-owning stage.</param>
/// <param name="Outcome">Winner or Loser.</param>
/// <param name="DestinationStageId">Destination stage.</param>
/// <param name="DestinationSlotKey">Destination slot key.</param>
public sealed record ProgressionPathRequest(
    Guid SourceFixtureId,
    ProgressionOutcome Outcome,
    Guid DestinationStageId,
    string DestinationSlotKey);
