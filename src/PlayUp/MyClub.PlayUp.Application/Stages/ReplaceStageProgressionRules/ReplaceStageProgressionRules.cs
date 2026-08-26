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
/// </summary>
public static class ReplaceStageProgressionRules
{
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

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Progression rules cannot be replaced while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.OrganisationNotMutable);
        }

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

            domainPaths.Add(
                new ProgressionPath(
                    spec.SourceFixtureId,
                    spec.Outcome,
                    new ProgressionDestination(spec.DestinationStageId, spec.DestinationSlotKey)));
        }

        stage.ReplaceProgressionRules(new ProgressionRules(domainPaths), clock);
    }
}

/// <summary>
/// Application DTO for one progression path (not a Domain VO).
/// </summary>
/// <param name="SourceFixtureId">Source fixture on the rules-owning stage.</param>
/// <param name="Outcome">Winner or Loser.</param>
/// <param name="DestinationStageId">Destination stage.</param>
/// <param name="DestinationSlotKey">Destination slot key.</param>
public sealed record ProgressionPathSpec(
    FixtureId SourceFixtureId,
    ProgressionOutcome Outcome,
    StageId DestinationStageId,
    string DestinationSlotKey);
