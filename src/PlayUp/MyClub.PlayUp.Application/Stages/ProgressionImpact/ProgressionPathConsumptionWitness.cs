// -----------------------------------------------------------------------
// <copyright file="ProgressionPathConsumptionWitness.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages.ProgressionImpact;

/// <summary>
/// WC-1 witness (A-3-R1), evaluated by <see cref="StructureProgressionImpactGate"/>.
/// </summary>
internal static class ProgressionPathConsumptionWitness
{
    internal enum Verdict
    {
        NotConsumed,
        ConsumedLoss,
        ConsumedAmbiguous
    }

    /// <summary>
    /// Evaluates WC-1 for one touched <paramref name="pathBefore"/> against proposed <paramref name="afterRules"/>.
    /// </summary>
    internal static Verdict EvaluateTouchedPath(
        ProgressionPath pathBefore,
        ProgressionRules? afterRules,
        Stage sourceStage,
        Stage destinationStage,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage)
    {
        if (!TryResolveExpectedEntryFromReplay(pathBefore, sourceStage, matchSummariesByStage, out var expectedEntry))
        {
            return DestinationShowsSatisfactionForPath(pathBefore, sourceStage.Id, destinationStage)
                ? Verdict.ConsumedAmbiguous
                : Verdict.NotConsumed;
        }

        var satBefore = DestinationSatisfaction.EvaluateProgression(
            destinationStage,
            sourceStage.Id,
            pathBefore,
            expectedEntry);
        if (satBefore != DestinationSatisfaction.Kind.Satisfied)
        {
            return Verdict.NotConsumed;
        }

        var afterPath = FindPathBySourceKey(afterRules, pathBefore);
        return afterPath?.Equals(pathBefore) != true
            ? Verdict.ConsumedLoss
            : Verdict.NotConsumed;
    }

    /// <summary>
    /// Worst verdict across all touched paths (for future gate).
    /// </summary>
    internal static Verdict EvaluateTouchedPaths(
        IReadOnlyList<ProgressionPath> touchedPaths,
        ProgressionRules? afterRules,
        Stage sourceStage,
        IReadOnlyDictionary<StageId, Stage> destinationStagesById,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage)
    {
        var worst = Verdict.NotConsumed;
        foreach (var pathBefore in touchedPaths)
        {
            if (!destinationStagesById.TryGetValue(pathBefore.Destination.StageId, out var destination))
            {
                worst = Max(worst, Verdict.ConsumedAmbiguous);
                continue;
            }

            var verdict = EvaluateTouchedPath(
                pathBefore,
                afterRules,
                sourceStage,
                destination,
                matchSummariesByStage);
            worst = Max(worst, verdict);
            if (worst == Verdict.ConsumedLoss)
            {
                return worst;
            }
        }

        return worst;
    }

    private static Verdict Max(Verdict a, Verdict b) =>
        (Verdict)Math.Max((int)a, (int)b);

    private static ProgressionPath? FindPathBySourceKey(ProgressionRules? afterRules, ProgressionPath pathBefore) => afterRules?.Paths.FirstOrDefault(candidate => candidate.SourcePairKey == pathBefore.SourcePairKey && candidate.Outcome == pathBefore.Outcome);

    private static bool TryResolveExpectedEntryFromReplay(
        ProgressionPath path,
        Stage sourceStage,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage,
        out EntryId expectedEntry)
    {
        var projected = InboundFeedSeatResolver.ProjectProgressionEntryFromSourceFixture(
            path,
            sourceStage,
            matchSummariesByStage);
        if (projected is null)
        {
            expectedEntry = default;
            return false;
        }

        expectedEntry = projected.Value;
        return true;
    }

    private static bool DestinationShowsSatisfactionForPath(
        ProgressionPath path,
        StageId sourceStageId,
        Stage destinationStage) =>
        destinationStage.CompositionEntries.Any(entry => DestinationSatisfaction.EvaluateProgression(
                                                             destinationStage,
                                                             sourceStageId,
                                                             path,
                                                             entry.EntryId)
                                                         == DestinationSatisfaction.Kind.Satisfied) || (path.Destination.TargetsSlot
              && destinationStage.FindSlot(path.Destination.SlotKey)?.EntryId is { } slotEntry
              && DestinationSatisfaction.EvaluateProgression(
                  destinationStage,
                  sourceStageId,
                  path,
                  slotEntry)
              == DestinationSatisfaction.Kind.Satisfied);
}
