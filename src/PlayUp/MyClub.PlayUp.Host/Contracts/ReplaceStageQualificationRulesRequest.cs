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
    string? DestinationSlotKey = null,
    IReadOnlyList<Guid>? DestinationGroupIds = null,
    bool DestinationForm = false);

/// <summary>
/// One HTTP qualification path (destination = peer Population, Form, Cup Place, or Groups Place).
/// </summary>
/// <param name="Order">Processing / display order (≥ 1).</param>
/// <param name="SelectionMode">How participants are selected from the ranking (Position, Top/Best, Bottom/Worst, Range).</param>
/// <param name="SelectionValue">Position, count, or range lower bound (depends on <paramref name="SelectionMode"/>).</param>
/// <param name="DestinationStageId">Destination stage.</param>
/// <param name="RankingScope">Optional ranking scope (Overall, Group, AcrossGroups); omit when not applicable.</param>
/// <param name="GroupId">Source group when scope is Group; omit or null otherwise.</param>
/// <param name="AcrossGroupsPosition">Across-groups position when scope is AcrossGroups; omit or null otherwise.</param>
/// <param name="SelectionEndValue">Inclusive range upper bound when mode is Range; omit or null otherwise.</param>
/// <param name="MinimumPoints">Optional Points ≥ gate (Position selection only in V1); omit or null when none.</param>
/// <param name="DestinationSlotKey">Slot key for Cup Place; omit or null when not slot-targeting.</param>
/// <param name="DestinationGroupId">Group id for Groups Place; omit or null when not group-targeting.</param>
/// <param name="DestinationForm">True for Form Placement (Championship/Swiss).</param>
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
    string? DestinationSlotKey = null,
    Guid? DestinationGroupId = null,
    bool DestinationForm = false);
