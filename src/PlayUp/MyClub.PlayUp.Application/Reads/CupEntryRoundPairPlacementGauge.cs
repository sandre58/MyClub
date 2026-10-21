// -----------------------------------------------------------------------
// <copyright file="CupEntryRoundPairPlacementGauge.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Matchs prerequisite gauge — entry-round <em>pairs</em> with both placement slots filled (schematic),
/// independent of slot <see cref="Slot.EntryId"/> resolution (see Qualifiés « Résolues ») and of Start gates.
/// </summary>
internal static class CupEntryRoundPairPlacementGauge
{
    internal static (int Filled, int Capacity)? TryMeasure(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);

        var pairs = ResolveEntryRoundPairs(stage);
        if (pairs.Count == 0)
        {
            return null;
        }

        var schematic = StageSchematicAssembler.Assemble(stage, competition, competitionStages);
        var filled = pairs.Count(pair => IsPairPlacementComplete(schematic, pair));
        return filled == 0 && pairs.All(pair => !PairPlacementStarted(schematic, pair)) ? null : (filled, pairs.Count);
    }

    /// <summary>
    /// Canonical entry-round progress for hub gauges (all expected pairs; unscaffolded pairs count as incomplete).
    /// </summary>
    internal static bool TryGetCanonicalEntryRoundDisplayProgress(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        out int canonicalPairCount,
        out int placementIncompletePairCount)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);

        var pairs = ResolveCanonicalPairsForPlacement(stage);
        if (pairs.Count == 0)
        {
            canonicalPairCount = 0;
            placementIncompletePairCount = 0;
            return false;
        }

        var schematic = StageSchematicAssembler.Assemble(stage, competition, competitionStages);
        canonicalPairCount = pairs.Count;
        placementIncompletePairCount = pairs.Count(pair => !IsPairPlacementComplete(schematic, pair));
        return true;
    }

    internal static bool HasIncompleteStartedPairPlacement(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages) =>
        TryGetCanonicalEntryRoundDisplayProgress(
            competition,
            stage,
            competitionStages,
            out _,
            out var incomplete)
        && incomplete > 0;

    internal static bool HasRemainingCanonicalEntryRoundPairs(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);

        var pairs = CupBracketTopologyClassifier.ExpectedCanonicalPairs(stage);
        if (pairs.Count == 0)
        {
            return false;
        }

        var schematic = StageSchematicAssembler.Assemble(stage, competition, competitionStages);
        var startedPairs = pairs.Where(pair => PairPlacementStarted(schematic, pair)).ToArray();
        return startedPairs.Length != 0 && startedPairs.All(pair => IsPairPlacementComplete(schematic, pair)) && pairs.Count > startedPairs.Length;
    }

    /// <summary>
    /// Canonical entry-round progress aligned with the operational « Paires » gauge (placement, not team resolution).
    /// </summary>
    internal static bool TryGetCanonicalEntryRoundProgress(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        out int canonicalPairCount,
        out int placementIncompletePairCount)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);

        var pairs = ResolveCanonicalPairsForPlacement(stage);
        if (pairs.Count == 0)
        {
            canonicalPairCount = 0;
            placementIncompletePairCount = 0;
            return false;
        }

        var schematic = StageSchematicAssembler.Assemble(stage, competition, competitionStages);
        var startedPairs = pairs.Where(pair => PairPlacementStarted(schematic, pair)).ToArray();
        if (startedPairs.Length == 0)
        {
            canonicalPairCount = 0;
            placementIncompletePairCount = 0;
            return false;
        }

        var filled = startedPairs.Count(pair => IsPairPlacementComplete(schematic, pair));
        canonicalPairCount = startedPairs.Length;
        placementIncompletePairCount = startedPairs.Length - filled;
        return true;
    }

    private static bool PairPlacementStarted(StageSchematicDto schematic, BracketPair pair) =>
        IsCupSlotPlacementFilled(schematic, pair.SlotAKey)
        || IsCupSlotPlacementFilled(schematic, pair.SlotBKey);

    private static bool IsPairPlacementComplete(StageSchematicDto schematic, BracketPair pair) =>
        IsCupSlotPlacementFilled(schematic, pair.SlotAKey)
        && IsCupSlotPlacementFilled(schematic, pair.SlotBKey);

    private static IReadOnlyList<BracketPair> ResolveEntryRoundPairs(Stage stage)
    {
        var canonical = ResolveCanonicalPairsForPlacement(stage);
        return canonical.Count > 0 ? canonical : stage.BracketPairs.Count > 0 ? stage.BracketPairs : [];
    }

    /// <summary>
    /// Canonical entry-round pairs for placement gauges; when topology is incomplete, only pairs already on the stage count.
    /// </summary>
    private static IReadOnlyList<BracketPair> ResolveCanonicalPairsForPlacement(Stage stage)
    {
        var expected = CupBracketTopologyClassifier.ExpectedCanonicalPairs(stage);
        if (expected.Count == 0 || CupBracketTopologyClassifier.Classify(stage) is not CupBracketTopologyKind.CanonicalIncomplete)
        {
            return expected;
        }

        var presentKeys = stage.BracketPairs
            .Select(pair => pair.PairKey)
            .ToHashSet(StringComparer.Ordinal);
        return [.. expected.Where(pair => presentKeys.Contains(pair.PairKey))];
    }

    private static bool IsCupSlotPlacementFilled(StageSchematicDto schematic, string slotKey) => (from place in schematic.Cases where place.FormPosition.Kind == StageSchematicAssembler.FormKindCupSlot && string.Equals(place.FormPosition.SlotKey, slotKey, StringComparison.Ordinal) select StructurePlaceOccupancyIssues.IsSchematicCaseOccupied(place)).FirstOrDefault();
}
