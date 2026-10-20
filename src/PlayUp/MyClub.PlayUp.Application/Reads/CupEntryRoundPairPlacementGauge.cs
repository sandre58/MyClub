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
        return (filled, pairs.Count);
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

        var pairs = CupBracketTopologyClassifier.ExpectedCanonicalPairs(stage);
        if (pairs.Count == 0)
        {
            canonicalPairCount = 0;
            placementIncompletePairCount = 0;
            return false;
        }

        var schematic = StageSchematicAssembler.Assemble(stage, competition, competitionStages);
        var filled = pairs.Count(pair => IsPairPlacementComplete(schematic, pair));
        canonicalPairCount = pairs.Count;
        placementIncompletePairCount = pairs.Count - filled;
        return true;
    }

    private static bool IsPairPlacementComplete(StageSchematicDto schematic, BracketPair pair) =>
        IsCupSlotPlacementFilled(schematic, pair.SlotAKey)
        && IsCupSlotPlacementFilled(schematic, pair.SlotBKey);

    private static IReadOnlyList<BracketPair> ResolveEntryRoundPairs(Stage stage)
    {
        var canonical = CupBracketTopologyClassifier.ExpectedCanonicalPairs(stage);
        if (canonical.Count > 0)
        {
            return canonical;
        }

        return stage.BracketPairs.Count > 0 ? stage.BracketPairs : [];
    }

    private static bool IsCupSlotPlacementFilled(StageSchematicDto schematic, string slotKey)
    {
        foreach (var place in schematic.Cases)
        {
            if (place.FormPosition.Kind != StageSchematicAssembler.FormKindCupSlot
                || !string.Equals(place.FormPosition.SlotKey, slotKey, StringComparison.Ordinal))
            {
                continue;
            }

            return StructurePlaceOccupancyIssues.IsSchematicCaseOccupied(place);
        }

        return false;
    }
}
