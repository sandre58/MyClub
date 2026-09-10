// -----------------------------------------------------------------------
// <copyright file="ReplaceStagePlacementAwardRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: replace PlacementAwardRules on a Stage (thin authoring — configuration only).
/// Does not resolve awards, mutate slots, or compute CompetitionOutcome.
/// </summary>
public static class ReplaceStagePlacementAwardRules
{
    /// <summary>
    /// Replaces placement award paths on the stage (null/empty clears rules).
    /// </summary>
    public static void Execute(
        Stage stage,
        IReadOnlyList<PlacementAwardPathSpec>? paths,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Placement award rules cannot be replaced while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        if (paths is null || paths.Count == 0)
        {
            stage.ReplacePlacementAwardRules(null, clock);
            return;
        }

        var domainPaths = new List<PlacementAwardPath>(paths.Count);
        foreach (var spec in paths)
        {
            ArgumentNullException.ThrowIfNull(spec);
            if (!Enum.IsDefined(spec.Outcome))
            {
                throw new ApplicationFailureException(
                    $"Unknown placement award outcome '{spec.Outcome}'.",
                    ApplicationErrorCodes.InvalidStructureIntent);
            }

            domainPaths.Add(new PlacementAwardPath(spec.SourceFixtureId, spec.Outcome, spec.Rank));
        }

        stage.ReplacePlacementAwardRules(new PlacementAwardRules(domainPaths), clock);
    }
}

/// <summary>
/// Application DTO for one placement award path (not a Domain VO).
/// </summary>
/// <param name="SourceFixtureId">Source fixture on the rules-owning stage.</param>
/// <param name="Outcome">Winner or Loser.</param>
/// <param name="Rank">1-based final competition rank awarded.</param>
public sealed record PlacementAwardPathSpec(
    FixtureId SourceFixtureId,
    ProgressionOutcome Outcome,
    int Rank);
