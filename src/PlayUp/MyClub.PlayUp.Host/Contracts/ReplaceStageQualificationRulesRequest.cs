// -----------------------------------------------------------------------
// <copyright file="ReplaceStageQualificationRulesRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for replacing qualification rules on a stage.
/// Prefer <see cref="Intents"/>; <see cref="Paths"/> remains for legacy clients.
/// </summary>
/// <param name="Intents">Authoring intents (empty or null with empty paths clears rules).</param>
/// <param name="Paths">Legacy atomic paths when intents are omitted.</param>
public sealed record ReplaceStageQualificationRulesRequest(
    IReadOnlyList<QualificationIntentRequest>? Intents = null,
    IReadOnlyList<QualificationPathRequest>? Paths = null);

/// <summary>
/// One HTTP qualification authoring intent.
/// </summary>
public sealed record QualificationIntentRequest(
    Guid IntentId,
    int Order,
    QualificationIntentSourceKind SourceKind,
    int PositionFrom,
    int PositionTo,
    Guid DestinationStageId,
    QualificationMappingMode MappingMode = QualificationMappingMode.Canonical,
    Guid? GroupId = null,
    int? AcrossGroupsPosition = null,
    int? MinimumPoints = null,
    IReadOnlyList<QualificationSlotOverrideRequest>? SlotOverrides = null);

/// <summary>
/// One HTTP slot override (Custom mapping).
/// </summary>
public sealed record QualificationSlotOverrideRequest(
    RankingScope Scope,
    int Position,
    string SlotKey,
    Guid? GroupId = null,
    int? AcrossGroupsPosition = null);

/// <summary>
/// One HTTP qualification path.
/// </summary>
public sealed record QualificationPathRequest(
    int Order,
    SelectionMode SelectionMode,
    int SelectionValue,
    Guid DestinationStageId,
    string DestinationSlotKey,
    RankingScope? RankingScope = null,
    Guid? GroupId = null,
    int? AcrossGroupsPosition = null,
    int? SelectionEndValue = null,
    int? MinimumPoints = null);
