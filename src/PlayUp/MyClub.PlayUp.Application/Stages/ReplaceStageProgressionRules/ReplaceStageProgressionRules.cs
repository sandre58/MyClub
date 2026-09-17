// -----------------------------------------------------------------------
// <copyright file="ReplaceStageProgressionRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: replace ProgressionRules on a Stage (thin authoring).
/// Prefers <see cref="ProgressionIntentSpec"/> when provided; otherwise path specs.
/// </summary>
public static class ReplaceStageProgressionRules
{
    /// <summary>
    /// Replaces progression from authoring intents (null/empty clears).
    /// </summary>
    public static void Execute(
        Stage stage,
        IReadOnlyList<ProgressionIntentSpec>? intents,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureMutable(stage);

        if (intents is null || intents.Count == 0)
        {
            stage.ReplaceProgressionRules(null, clock);
            return;
        }

        var domainIntents = new List<ProgressionIntent>(intents.Count);
        foreach (var spec in intents)
        {
            ArgumentNullException.ThrowIfNull(spec);
            domainIntents.Add(ToDomainIntent(stage, spec));
        }

        stage.ReplaceProgressionRules(
            ProgressionRules.FromIntents(domainIntents, stage.Rounds),
            clock);
    }

    /// <summary>
    /// Replaces progression paths on the stage (null/empty clears rules).
    /// </summary>
    public static void Execute(
        Stage stage,
        IReadOnlyList<ProgressionPathSpec>? paths,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureMutable(stage);

        if (paths is null || paths.Count == 0)
        {
            stage.ReplaceProgressionRules(null, clock);
            return;
        }

        var domainPaths = new List<ProgressionPath>(paths.Count);
        foreach (var spec in paths)
        {
            ArgumentNullException.ThrowIfNull(spec);
            if (!Enum.IsDefined(spec.Outcome))
            {
                throw new ApplicationFailureException(
                    $"Unknown progression outcome '{spec.Outcome}'.",
                    ApplicationErrorCodes.InvalidStructureIntent);
            }

            var destination = string.IsNullOrWhiteSpace(spec.DestinationSlotKey)
                ? ProgressionDestination.ForPopulation(spec.DestinationStageId)
                : ProgressionDestination.ForSlot(spec.DestinationStageId, spec.DestinationSlotKey);

            domainPaths.Add(
                new ProgressionPath(
                    spec.SourceFixtureId,
                    spec.Outcome,
                    destination));
        }

        stage.ReplaceProgressionRules(new ProgressionRules(domainPaths), clock);
    }

    private static void EnsureMutable(Stage stage)
    {
        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Progression rules cannot be replaced while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }
    }

    private static ProgressionIntent ToDomainIntent(Stage stage, ProgressionIntentSpec spec)
    {
        if (!Enum.IsDefined(spec.Outcome))
        {
            throw new ApplicationFailureException(
                $"Unknown progression outcome '{spec.Outcome}'.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        if (!stage.HasRound(spec.RoundId))
        {
            throw new ApplicationFailureException(
                $"Round '{spec.RoundId}' was not found on the stage.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        var destination = string.IsNullOrWhiteSpace(spec.DestinationSlotKey)
            ? ProgressionDestination.ForPopulation(spec.DestinationStageId)
            : ProgressionDestination.ForSlot(spec.DestinationStageId, spec.DestinationSlotKey);

        var intentId = spec.IntentId is { } guid && guid != Guid.Empty
            ? new IntentId(guid)
            : IntentId.New();

        return new ProgressionIntent(
            intentId,
            spec.Order,
            spec.RoundId,
            spec.Outcome,
            destination);
    }
}

/// <summary>
/// Application DTO for one progression path (not a Domain VO).
/// </summary>
public sealed record ProgressionPathSpec(
    FixtureId SourceFixtureId,
    ProgressionOutcome Outcome,
    StageId DestinationStageId,
    string? DestinationSlotKey);

/// <summary>
/// Application DTO for one progression intent (Round × Outcome → Destination).
/// </summary>
public sealed record ProgressionIntentSpec(
    Guid? IntentId,
    int Order,
    RoundId RoundId,
    ProgressionOutcome Outcome,
    StageId DestinationStageId,
    string? DestinationSlotKey = null);
