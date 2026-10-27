// -----------------------------------------------------------------------
// <copyright file="CupCanonicalEntryRoundStartGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Cup Start guard: canonical mono-round entry pairs must have both placement slots filled before Running.
/// </summary>
internal static class CupCanonicalEntryRoundStartGate
{
    internal const string DisabledReasonCode = "IncompleteEntryRound";

    internal static bool BlocksStart(
        Stage stage,
        Competition? competition = null,
        IReadOnlyList<Stage>? competitionStages = null)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (competition is null || competitionStages is null)
        {
            return TryGetCanonicalEntryRoundProgress(stage, out var canonical, out var placementIncomplete) && canonical > 0 ? placementIncomplete > 0 : StructureSkeleton.InferFormat(stage) == StructureFormatKind.Cup && stage.Status == StageStatus.Ready && HasUndeterminedBracketPairs(stage);
        }

        if (CupEntryRoundPairPlacementGauge.TryGetCanonicalEntryRoundDisplayProgress(
                competition,
                stage,
                competitionStages,
                out var canonicalPairCount,
                out var incompleteCanonicalPairs)
            && canonicalPairCount > 0)
        {
            return incompleteCanonicalPairs > 0;
        }

        var explicitMeasure = CupEntryRoundPairPlacementGauge.TryMeasure(
            competition,
            stage,
            competitionStages);
        return explicitMeasure is { Capacity: > 0 } && explicitMeasure.Value.Filled < explicitMeasure.Value.Capacity;
    }

    internal static bool HasUndeterminedBracketPairs(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        return stage.BracketPairs.Count != 0 && stage.BracketPairs.Any(pair => !IsPairDetermined(stage, pair));
    }

    /// <summary>
    /// Canonical mono-round Cup entry-round progress for Structure workspace advisories (UX tranche 2).
    /// </summary>
    internal static bool TryGetCanonicalEntryRoundProgress(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        out int canonicalPairCount,
        out int placementIncompletePairCount) =>
        CupEntryRoundPairPlacementGauge.TryGetCanonicalEntryRoundProgress(
            competition,
            stage,
            competitionStages,
            out canonicalPairCount,
            out placementIncompletePairCount);

    /// <summary>
    /// Stage-only fallback when competition context is unavailable (operational edit guards).
    /// </summary>
    internal static bool TryGetCanonicalEntryRoundProgress(
        Stage stage,
        out int canonicalPairCount,
        out int placementIncompletePairCount)
    {
        ArgumentNullException.ThrowIfNull(stage);
        canonicalPairCount = 0;
        var pairs = CupBracketTopologyClassifier.ExpectedCanonicalPairs(stage);
        if (pairs.Count == 0)
        {
            placementIncompletePairCount = 0;
            return false;
        }

        canonicalPairCount = pairs.Count;
        placementIncompletePairCount = pairs.Count(pair => !IsPairPlacementComplete(stage, pair));

        return true;
    }

    private static bool IsPairPlacementComplete(Stage stage, BracketPair pair) =>
        IsSlotPlacementFilled(stage, pair.SlotAKey)
        && IsSlotPlacementFilled(stage, pair.SlotBKey);

    private static bool IsSlotPlacementFilled(Stage stage, string slotKey) => stage.FindSlot(slotKey)?.EntryId is not null || stage.DirectAssignments.Any(a => string.Equals(a.SlotKey, slotKey, StringComparison.Ordinal));

    private static bool IsPairDetermined(Stage stage, BracketPair pair)
    {
        var slotA = stage.FindSlot(pair.SlotAKey);
        var slotB = stage.FindSlot(pair.SlotBKey);
        return slotA?.EntryId is { } home
               && slotB?.EntryId is { } away
               && !home.Equals(away);
    }
}
