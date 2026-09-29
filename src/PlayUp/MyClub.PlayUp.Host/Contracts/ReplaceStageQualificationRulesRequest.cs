// -----------------------------------------------------------------------
// <copyright file="ReplaceStageQualificationRulesRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for replacing qualification rules on a stage (intents authoring SoT).
/// </summary>
/// <param name="Intents">Authoring intents; null or empty clears rules.</param>
public sealed record ReplaceStageQualificationRulesRequest(
    IReadOnlyList<QualificationIntentRequest>? Intents = null);

/// <summary>
/// One HTTP qualification authoring intent (destination = peer Population or Place).
/// </summary>
public sealed record QualificationIntentRequest(
    Guid IntentId,
    int Order,
    QualificationIntentSourceKind SourceKind,
    int PositionFrom,
    int PositionTo,
    Guid DestinationStageId,
    Guid? GroupId = null,
    int? AcrossGroupsPosition = null,
    int? MinimumPoints = null,
    IReadOnlyList<string>? DestinationSlotKeys = null,
    IReadOnlyList<Guid>? DestinationGroupIds = null,
    bool DestinationForm = false);
