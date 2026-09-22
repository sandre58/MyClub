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
/// Prefer <c>DestinationSlotKeys</c>; legacy <c>DestinationSlotKey</c> coerces to a one-element list.
/// </summary>
public sealed record ProgressionIntentRequest(
    Guid? IntentId,
    int Order,
    Guid RoundId,
    ProgressionOutcome Outcome,
    Guid DestinationStageId,
    IReadOnlyList<string>? DestinationSlotKeys = null,
    string? DestinationSlotKey = null,
    IReadOnlyList<Guid>? DestinationGroupIds = null,
    bool DestinationForm = false);

/// <summary>
/// One HTTP progression path.
/// </summary>
/// <param name="SourceFixtureId">Fixture on the rules-owning stage.</param>
/// <param name="Outcome">Winner or Loser.</param>
/// <param name="DestinationStageId">Destination stage.</param>
/// <param name="DestinationSlotKey">Cup Place slot key; omit or null when not slot-targeting.</param>
/// <param name="DestinationGroupId">Groups Place group id; omit or null when not group-targeting.</param>
/// <param name="DestinationForm">True for Form Placement.</param>
public sealed record ProgressionPathRequest(
    Guid SourceFixtureId,
    ProgressionOutcome Outcome,
    Guid DestinationStageId,
    string? DestinationSlotKey = null,
    Guid? DestinationGroupId = null,
    bool DestinationForm = false);
