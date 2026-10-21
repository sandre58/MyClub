// -----------------------------------------------------------------------
// <copyright file="StructureProgressionImpactGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Stages.ProgressionImpact;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// G-OPS3 progression impact (W-OPS3-2) — WC-1 consumption and WC-2 downstream production participation.
/// </summary>
internal static class StructureProgressionImpactGate
{
    internal static void EnsureReplaceAllowed(
        Stage upstream,
        ProgressionRules? afterRules,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyList<Match>? upstreamMatches,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>>? destinationMatchesByStage)
    {
        ArgumentNullException.ThrowIfNull(upstream);
        ArgumentNullException.ThrowIfNull(competitionStages);

        var beforeRules = upstream.Regulation.ProgressionRules;
        if (Equals(beforeRules, afterRules))
        {
            return;
        }

        var diff = ProgressionRulesPathDiff.Compare(beforeRules, afterRules);
        if (diff.Touched.Count == 0)
        {
            return;
        }

        var summaries = ProgressionImpactMatchSummaries.ForUpstreamStage(upstream, upstreamMatches);
        var destinations = competitionStages.ToDictionary(stage => stage.Id);

        foreach (var pathBefore in diff.Touched)
        {
            if (!destinations.TryGetValue(pathBefore.Destination.StageId, out var destination))
            {
                throw new ApplicationFailureException(
                    "Progression impact cannot be evaluated: destination stage was not found.",
                    ApplicationErrorCodes.ProgressionFeedImpactAmbiguous);
            }

            var wc1 = ProgressionPathConsumptionWitness.EvaluateTouchedPath(
                pathBefore,
                afterRules,
                upstream,
                destination,
                summaries);
            ThrowForWc1(wc1);

            var destinationMatches = ProgressionImpactMatchSummaries.MatchesForStage(
                destination.Id,
                destinationMatchesByStage);
            var participants = MaterializedParticipantSnapshots.FromMatches(destinationMatches);
            var wc2 = ProgressionPathProductionParticipationWitness.EvaluateTouchedPath(
                pathBefore,
                upstream,
                destination,
                competitionStages,
                participants,
                summaries);
            ThrowForWc2(wc2, destination.Id);
        }
    }

    private static void ThrowForWc1(ProgressionPathConsumptionWitness.Verdict verdict) =>
        _ = verdict switch
        {
            ProgressionPathConsumptionWitness.Verdict.ConsumedLoss => throw new ApplicationFailureException(
                "Progression rules cannot change: a touched path has already satisfied its destination.",
                ApplicationErrorCodes.ProgressionFeedConsumed),
            ProgressionPathConsumptionWitness.Verdict.ConsumedAmbiguous => throw new ApplicationFailureException(
                "Progression rules cannot change: consumption of a touched path cannot be determined.",
                ApplicationErrorCodes.ProgressionFeedImpactAmbiguous),
            _ => (object?)null
        };

    private static void ThrowForWc2(
        ProgressionPathProductionParticipationWitness.Verdict verdict,
        StageId destinationId) =>
        _ = verdict switch
        {
            ProgressionPathProductionParticipationWitness.Verdict.Participating => throw new ApplicationFailureException(
                "Progression rules cannot change: a touched path participates in downstream match production.",
                ApplicationErrorCodes.ProgressionDownstreamProductionBlocked,
                [destinationId.Value.ToString()]),
            ProgressionPathProductionParticipationWitness.Verdict.ParticipationAmbiguous => throw new ApplicationFailureException(
                "Progression rules cannot change: downstream production link for a touched path cannot be determined.",
                ApplicationErrorCodes.ProgressionProductionParticipationAmbiguous,
                [destinationId.Value.ToString()]),
            _ => (object?)null
        };
}
