// -----------------------------------------------------------------------
// <copyright file="ReplaceStageQualificationRulesRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for replacing qualification rules on a stage.
/// Prefer <see cref="Intents"/>; <see cref="Paths"/> remains for path-list clients.
/// </summary>
/// <param name="Intents">Authoring intents (empty or null with empty paths clears rules).</param>
/// <param name="Paths">Atomic paths when intents are omitted.</param>
public sealed record ReplaceStageQualificationRulesRequest(
    IReadOnlyList<QualificationIntentRequest>? Intents = null,
    IReadOnlyList<QualificationPathRequest>? Paths = null);

/// <summary>
/// One HTTP qualification authoring intent (destination = peer Population or Place).
/// Prefer <c>DestinationSlotKeys</c>; legacy <c>DestinationSlotKey</c> coerces to a one-element list.
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
    string? DestinationSlotKey = null);

/// <summary>
/// One HTTP qualification path (destination = peer Population or Place).
/// </summary>
/// <param name="DestinationSlotKey">Slot key for Place (Auto); omit or null for Population.</param>
public sealed record QualificationPathRequest(
    int Order,
    SelectionMode SelectionMode,
    int SelectionValue,
    Guid DestinationStageId,
    RankingScope? RankingScope = null,
    Guid? GroupId = null,
    int? AcrossGroupsPosition = null,
    int? SelectionEndValue = null,
    int? MinimumPoints = null,
    string? DestinationSlotKey = null);
