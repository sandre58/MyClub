// -----------------------------------------------------------------------
// <copyright file="ReplaceStageQualificationRulesRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for replacing qualification rules on a stage.
/// </summary>
/// <param name="Paths">Replacement paths (empty or null clears rules).</param>
public sealed record ReplaceStageQualificationRulesRequest(
    IReadOnlyList<QualificationPathRequest>? Paths);

/// <summary>
/// One HTTP qualification path.
/// </summary>
/// <param name="Order">Path order (≥ 1).</param>
/// <param name="SelectionMode">How participants are selected.</param>
/// <param name="SelectionValue">Position, count, or range lower bound.</param>
/// <param name="DestinationStageId">Destination stage.</param>
/// <param name="DestinationSlotKey">Destination slot key.</param>
/// <param name="RankingScope">Optional ranking scope.</param>
/// <param name="GroupId">Group when scope is Group.</param>
/// <param name="AcrossGroupsPosition">Across-groups position when applicable.</param>
/// <param name="SelectionEndValue">Range upper bound when mode is Range.</param>
/// <param name="MinimumPoints">Optional Points ≥ gate.</param>
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
