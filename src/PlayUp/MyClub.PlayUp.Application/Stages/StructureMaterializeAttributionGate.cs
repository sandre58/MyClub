// -----------------------------------------------------------------------
// <copyright file="StructureMaterializeAttributionGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Refuses Cup materialization when requested or eligible production intersects
/// unreachable placement award keys (<see cref="CompetitionFlowAnalyzer.AuthoredAwardUnreachable"/>).
/// Scoped to the production being created — not a global Prepare attribution check.
/// </summary>
internal static class StructureMaterializeAttributionGate
{
    internal static void EnsureCupStage(Stage stage, IReadOnlyList<string>? pairKeys)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (!StageClassification.IsNonClassifyingPhase(stage))
        {
            return;
        }

        var unreachableKeys = CollectUnreachablePlacementPairKeys(stage);
        if (unreachableKeys.Count == 0)
        {
            return;
        }

        var requestEmpty = pairKeys is null || pairKeys.Count == 0;
        if (requestEmpty)
        {
            var eligibleKeys = MaterializeCupFromOccupiedSlots.CollectEligiblePairKeys(stage);
            if (!eligibleKeys.Any(unreachableKeys.Contains))
            {
                return;
            }
        }
        else if (!RequestedKeysIntersectUnreachable(pairKeys!, unreachableKeys))
        {
            return;
        }

        throw new ApplicationFailureException(
            $"Stage '{stage.Id}' materialization refused: placement award paths are unreachable for the requested production.",
            ApplicationErrorCodes.AttributionUnreachable);
    }

    private static bool RequestedKeysIntersectUnreachable(
        IReadOnlyList<string> pairKeys,
        HashSet<string> unreachableKeys)
    {
        foreach (var rawKey in pairKeys)
        {
            if (string.IsNullOrWhiteSpace(rawKey))
            {
                continue;
            }

            try
            {
                if (unreachableKeys.Contains(BracketPair.NormalizePairKey(rawKey)))
                {
                    return true;
                }
            }
            catch (DomainException)
            {
                // Invalid keys are handled later by materialize resolution.
            }
        }

        return false;
    }

    private static HashSet<string> CollectUnreachablePlacementPairKeys(Stage stage) =>
        CompetitionFlowAnalyzer.AnalyzeResult([stage])
            .Where(diagnostic =>
                diagnostic.Code == CompetitionFlowAnalyzer.AuthoredAwardUnreachable
                && diagnostic.StageId == stage.Id.Value
                && diagnostic.SlotKey is not null)
            .Select(diagnostic => diagnostic.SlotKey!)
            .ToHashSet(StringComparer.Ordinal);
}
