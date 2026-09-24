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
        EnsureMutable(stage);

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

            if (string.IsNullOrWhiteSpace(spec.SourcePairKey))
            {
                throw new ApplicationFailureException(
                    "Placement award path requires SourcePairKey.",
                    ApplicationErrorCodes.InvalidStructureIntent);
            }

            domainPaths.Add(new PlacementAwardPath(spec.SourcePairKey, spec.Outcome, spec.Rank));
        }

        stage.ReplacePlacementAwardRules(new PlacementAwardRules(domainPaths), clock);
    }

    private static void EnsureMutable(Stage stage)
    {
        if (stage.Status is StageStatus.Draft or StageStatus.Ready)
        {
            return;
        }

        throw new ApplicationFailureException(
            $"Stage '{stage.Id}' structure is not mutable in status '{stage.Status}'.",
            ApplicationErrorCodes.StructureNotMutable);
    }
}

/// <summary>
/// Application DTO for one placement award path (not a Domain VO).
/// </summary>
/// <param name="SourcePairKey">Structural source key (Cup = BracketPair.PairKey).</param>
/// <param name="Outcome">Winner or Loser.</param>
/// <param name="Rank">1-based final competition rank.</param>
public sealed record PlacementAwardPathSpec(
    string SourcePairKey,
    ProgressionOutcome Outcome,
    int Rank);
