// -----------------------------------------------------------------------
// <copyright file="ReplaceStageQualificationRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: replace QualificationRules on a Stage (thin authoring).
/// Prefers <see cref="QualificationIntentSpec"/> when provided; otherwise path specs.
/// </summary>
public static class ReplaceStageQualificationRules
{
    /// <summary>
    /// Replaces qualification rules from authoring intents (null/empty clears).
    /// </summary>
    public static void Execute(
        Stage sourceStage,
        IReadOnlyList<QualificationIntentSpec>? intents,
        IClock clock,
        IReadOnlyList<Stage>? competitionStages = null,
        IReadOnlyList<Match>? upstreamMatches = null)
    {
        ArgumentNullException.ThrowIfNull(sourceStage);
        ArgumentNullException.ThrowIfNull(clock);

        QualificationRules? afterRules;
        if (intents is null || intents.Count == 0)
        {
            afterRules = null;
        }
        else
        {
            var domainIntents = new List<QualificationIntent>(intents.Count);
            foreach (var spec in intents)
            {
                ArgumentNullException.ThrowIfNull(spec);
                domainIntents.Add(ToDomainIntent(spec));
            }

            var groupOrder = sourceStage.Groups.Select(g => g.Id).ToArray();
            afterRules = QualificationRules.FromIntents(domainIntents, groupOrder);
        }

        EnsureAndApply(sourceStage, afterRules, clock, competitionStages, upstreamMatches);
    }

    /// <summary>
    /// Replaces qualification paths on the stage (null/empty clears rules).
    /// </summary>
    public static void Execute(
        Stage stage,
        IReadOnlyList<QualificationPathSpec>? paths,
        IClock clock,
        IReadOnlyList<Stage>? competitionStages = null,
        IReadOnlyList<Match>? upstreamMatches = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        var afterRules = paths is null || paths.Count == 0 ? null : BuildRulesFromPaths(paths);
        EnsureAndApply(stage, afterRules, clock, competitionStages, upstreamMatches);
    }

    private static void EnsureAndApply(
        Stage stage,
        QualificationRules? afterRules,
        IClock clock,
        IReadOnlyList<Stage>? competitionStages,
        IReadOnlyList<Match>? upstreamMatches)
    {
        StructureOperationalExitEditGuard.Ensure(stage, upstreamMatches, "Qualification rules");
        if (competitionStages is not null)
        {
            StructureReadyConsumerFeedGate.EnsureQualificationReplaceAllowed(stage, afterRules, competitionStages);
        }

        stage.ReplaceQualificationRulesOperationally(afterRules, clock);
    }

    private static QualificationRules BuildRulesFromPaths(IReadOnlyList<QualificationPathSpec> paths)
    {
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
            var destStageId = new StageId(spec.DestinationStageId);
            var destination = ResolvePathDestination(
                destStageId,
                spec.DestinationSlotKey,
                spec.DestinationGroupId,
                spec.DestinationForm);
            var condition = spec.MinimumPoints is { } points
                ? QualificationCondition.PointsAtLeast(points)
                : null;

            domainPaths.Add(
                new QualificationPath(spec.Order, source, selection, destination, condition));
        }

        return new QualificationRules(domainPaths);
    }

    private static QualificationDestination ResolvePathDestination(
        StageId stageId,
        string? slotKey,
        Guid? destinationGroupId,
        bool destinationForm)
    {
        var hasSlot = !string.IsNullOrWhiteSpace(slotKey);
        var hasGroup = destinationGroupId is not null;
        var modes = (destinationForm ? 1 : 0) + (hasSlot ? 1 : 0) + (hasGroup ? 1 : 0);
        return modes > 1
            ? throw new ApplicationFailureException(
                "Qualification path destination must be exactly one of: population, form, slot, or group.",
                ApplicationErrorCodes.InvalidStructureIntent)
            : destinationForm
            ? QualificationDestination.ForForm(stageId)
            : hasGroup
            ? QualificationDestination.ForGroup(stageId, new GroupId(destinationGroupId!.Value))
            : hasSlot
            ? QualificationDestination.ForSlot(stageId, slotKey!)
            : QualificationDestination.ForPopulation(stageId);
    }

    private static QualificationIntent ToDomainIntent(QualificationIntentSpec spec) =>
        !Enum.IsDefined(spec.SourceKind)
            ? throw new ApplicationFailureException(
                $"Unknown qualification intent source kind '{spec.SourceKind}'.",
                ApplicationErrorCodes.InvalidStructureIntent)
            : new QualificationIntent(
                new IntentId(spec.IntentId),
                spec.Order,
                spec.SourceKind,
                spec.PositionFrom,
                spec.PositionTo,
                new StageId(spec.DestinationStageId),
                spec.GroupId is { } g ? new GroupId(g) : null,
                spec.AcrossGroupsPosition,
                spec.MinimumPoints is { } pts ? QualificationCondition.PointsAtLeast(pts) : null,
                spec.DestinationSlotKeys is { Count: > 0 } ? spec.DestinationSlotKeys : null,
                spec.DestinationGroupIds is { Count: > 0 } ? spec.DestinationGroupIds.Select(id => new GroupId(id)).ToArray() : null,
                spec.DestinationForm);
}

/// <summary>
/// Application DTO for one qualification path (not a Domain VO).
/// </summary>
public sealed record QualificationPathSpec(
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
    Guid? GroupId = null,
    int? AcrossGroupsPosition = null,
    int? MinimumPoints = null,
    IReadOnlyList<string>? DestinationSlotKeys = null,
    IReadOnlyList<Guid>? DestinationGroupIds = null,
    bool DestinationForm = false);
