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
/// Prefers <see cref="QualificationIntentSpec"/> when provided; otherwise legacy path specs.
/// </summary>
public static class ReplaceStageQualificationRules
{
    /// <summary>
    /// Replaces qualification rules from authoring intents (null/empty clears).
    /// </summary>
    public static void Execute(
        Stage sourceStage,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyList<QualificationIntentSpec>? intents,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(sourceStage);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureMutable(sourceStage);

        if (intents is null || intents.Count == 0)
        {
            sourceStage.ReplaceQualificationRules(null, clock);
            return;
        }

        var domainIntents = new List<QualificationIntent>(intents.Count);
        foreach (var spec in intents)
        {
            ArgumentNullException.ThrowIfNull(spec);
            domainIntents.Add(ToDomainIntent(spec));
        }

        var groupOrder = sourceStage.Groups.Select(g => g.Id).ToArray();
        var slotOrderByStage = competitionStages.ToDictionary(
            s => s.Id,
            s => (IReadOnlyList<string>)[.. s.Slots.Select(slot => slot.SlotKey)]);

        var rules = QualificationRules.FromIntents(domainIntents, groupOrder, slotOrderByStage);
        sourceStage.ReplaceQualificationRules(rules, clock);
    }

    /// <summary>
    /// Legacy: replaces qualification paths on the stage (null/empty clears rules).
    /// </summary>
    public static void Execute(
        Stage stage,
        IReadOnlyList<QualificationPathSpec>? paths,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureMutable(stage);

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
            var condition = spec.MinimumPoints is { } points
                ? QualificationCondition.PointsAtLeast(points)
                : null;

            domainPaths.Add(
                new QualificationPath(spec.Order, source, selection, destination, condition));
        }

        stage.ReplaceQualificationRules(new QualificationRules(domainPaths), clock);
    }

    private static void EnsureMutable(Stage stage)
    {
        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Qualification rules cannot be replaced while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }
    }

    private static QualificationIntent ToDomainIntent(QualificationIntentSpec spec)
    {
        if (!Enum.IsDefined(spec.SourceKind))
        {
            throw new ApplicationFailureException(
                $"Unknown qualification intent source kind '{spec.SourceKind}'.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        if (!Enum.IsDefined(spec.MappingMode))
        {
            throw new ApplicationFailureException(
                $"Unknown qualification mapping mode '{spec.MappingMode}'.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        var overrides = spec.SlotOverrides?
            .Select(o => new QualificationSlotOverride(
                new QualificationSourceOccurrence(
                    o.Scope,
                    o.Position,
                    o.GroupId is { } gid ? new GroupId(gid) : null,
                    o.AcrossGroupsPosition),
                o.SlotKey))
            .ToArray();

        return new QualificationIntent(
            new IntentId(spec.IntentId),
            spec.Order,
            spec.SourceKind,
            spec.PositionFrom,
            spec.PositionTo,
            new StageId(spec.DestinationStageId),
            spec.MappingMode,
            spec.GroupId is { } g ? new GroupId(g) : null,
            spec.AcrossGroupsPosition,
            spec.MinimumPoints is { } pts ? QualificationCondition.PointsAtLeast(pts) : null,
            overrides);
    }
}

/// <summary>
/// Application DTO for one qualification path (not a Domain VO).
/// </summary>
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

/// <summary>
/// Application DTO for one qualification authoring intent.
/// </summary>
public sealed record QualificationIntentSpec(
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
    IReadOnlyList<QualificationSlotOverrideSpec>? SlotOverrides = null);

/// <summary>
/// Application DTO for one slot override.
/// </summary>
public sealed record QualificationSlotOverrideSpec(
    RankingScope Scope,
    int Position,
    string SlotKey,
    Guid? GroupId = null,
    int? AcrossGroupsPosition = null);
