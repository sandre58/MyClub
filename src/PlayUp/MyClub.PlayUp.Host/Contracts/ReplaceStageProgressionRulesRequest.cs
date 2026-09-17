// -----------------------------------------------------------------------
// <copyright file="ReplaceStageProgressionRulesRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for replacing progression rules on a stage.
/// Prefer <see cref="Intents"/> (V3 authoring). Paths = atomic / legacy.
/// </summary>
/// <param name="Intents">Authoring intents (Round × Outcome → Destination).</param>
/// <param name="Paths">Replacement paths (empty or null clears when intents also empty).</param>
public sealed record ReplaceStageProgressionRulesRequest(
    IReadOnlyList<ProgressionIntentRequest>? Intents = null,
    IReadOnlyList<ProgressionPathRequest>? Paths = null);

/// <summary>
/// One HTTP progression intent.
/// </summary>
public sealed record ProgressionIntentRequest(
    Guid? IntentId,
    int Order,
    Guid RoundId,
    ProgressionOutcome Outcome,
    Guid DestinationStageId,
    string? DestinationSlotKey = null);

/// <summary>
/// One HTTP progression path.
/// </summary>
/// <param name="SourceFixtureId">Fixture on the rules-owning stage.</param>
/// <param name="Outcome">Winner or Loser.</param>
/// <param name="DestinationStageId">Destination stage.</param>
/// <param name="DestinationSlotKey">
/// Destination slot key for placement; omit or null for population target (O2-a).
/// </param>
public sealed record ProgressionPathRequest(
    Guid SourceFixtureId,
    ProgressionOutcome Outcome,
    Guid DestinationStageId,
    string? DestinationSlotKey = null);
