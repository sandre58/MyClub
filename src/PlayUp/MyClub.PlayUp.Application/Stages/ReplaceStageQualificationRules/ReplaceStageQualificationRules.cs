// -----------------------------------------------------------------------
// <copyright file="ReplaceStageQualificationRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: replace QualificationRules on a Stage (thin authoring).
/// </summary>
public static class ReplaceStageQualificationRules
{
    /// <summary>
    /// Replaces qualification paths on the stage (null/empty clears rules).
    /// </summary>
    public static void Execute(
        Stage stage,
        IReadOnlyList<QualificationPathSpec>? paths,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Qualification rules cannot be replaced while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        if (paths is null || paths.Count == 0)
        {
            stage.ReplaceQualificationRules(null, clock);
            return;
        }

        var domainPaths = new List<QualificationPath>(paths.Count);
        foreach (var spec in paths)
        {
            ArgumentNullException.ThrowIfNull(spec);
            if (!Enum.IsDefined(spec.SelectionMode))
            {
                throw new ApplicationFailureException(
                    $"Unknown qualification selection mode '{spec.SelectionMode}'.",
                    ApplicationErrorCodes.InvalidStructureIntent);
            }

            if (spec.RankingScope is { } scope && !Enum.IsDefined(scope))
            {
                throw new ApplicationFailureException(
                    $"Unknown ranking scope '{scope}'.",
                    ApplicationErrorCodes.InvalidStructureIntent);
            }

            GroupId? groupId = spec.GroupId is { } gid ? new GroupId(gid) : null;
            var source = new QualificationSource(spec.RankingScope, groupId, spec.AcrossGroupsPosition);
            var selection = new QualificationSelection(spec.SelectionMode, spec.SelectionValue, spec.SelectionEndValue);
            var destination = new QualificationDestination(
                new StageId(spec.DestinationStageId),
                spec.DestinationSlotKey);
            QualificationCondition? condition = spec.MinimumPoints is { } points
                ? QualificationCondition.PointsAtLeast(points)
                : null;

            domainPaths.Add(
                new QualificationPath(spec.Order, source, selection, destination, condition));
        }

        stage.ReplaceQualificationRules(new QualificationRules(domainPaths), clock);
    }
}

/// <summary>
/// Application DTO for one qualification path (not a Domain VO).
/// </summary>
/// <param name="Order">Processing / display order (≥ 1).</param>
/// <param name="SelectionMode">How participants are selected.</param>
/// <param name="SelectionValue">Position, count, or range lower bound.</param>
/// <param name="DestinationStageId">Destination stage.</param>
/// <param name="DestinationSlotKey">Destination slot key.</param>
/// <param name="RankingScope">Optional ranking scope; null implies overall.</param>
/// <param name="GroupId">Group when scope is Group.</param>
/// <param name="AcrossGroupsPosition">Position when scope is AcrossGroups.</param>
/// <param name="SelectionEndValue">Inclusive range upper bound when mode is Range.</param>
/// <param name="MinimumPoints">Optional Points ≥ gate (Position selection only).</param>
public sealed record QualificationPathSpec(
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
