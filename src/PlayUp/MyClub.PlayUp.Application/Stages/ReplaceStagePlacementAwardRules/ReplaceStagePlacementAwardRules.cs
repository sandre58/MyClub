// -----------------------------------------------------------------------
// <copyright file="ReplaceStagePlacementAwardRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
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
        IClock clock,
        IReadOnlyList<Match>? stageMatches = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);
        StructureOperationalExitEditGuard.Ensure(stage, stageMatches, "Placement award rules");

        if (paths is null || paths.Count == 0)
        {
            stage.ReplacePlacementAwardRulesOperationally(null, clock);
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

        stage.ReplacePlacementAwardRulesOperationally(new PlacementAwardRules(domainPaths), clock);
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
