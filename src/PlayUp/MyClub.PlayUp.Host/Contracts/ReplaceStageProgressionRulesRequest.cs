// -----------------------------------------------------------------------
// <copyright file="ReplaceStageProgressionRulesRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for replacing progression rules on a stage (intents authoring SoT).
/// </summary>
/// <param name="Intents">Authoring intents; null or empty clears rules.</param>
public sealed record ReplaceStageProgressionRulesRequest(
    IReadOnlyList<ProgressionIntentRequest>? Intents = null);

/// <summary>
/// One HTTP progression intent.
/// </summary>
public sealed record ProgressionIntentRequest(
    Guid? IntentId,
    int Order,
    Guid RoundId,
    ProgressionOutcome Outcome,
    Guid DestinationStageId,
    IReadOnlyList<string>? DestinationSlotKeys = null,
    IReadOnlyList<Guid>? DestinationGroupIds = null,
    bool DestinationForm = false);
