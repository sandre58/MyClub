// -----------------------------------------------------------------------
// <copyright file="StructureMatchProductionCapacity.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Match-slot capacity for production gauges (legs-aware); confrontations come from materialize reads.
/// </summary>
internal static class StructureMatchProductionCapacity
{
    internal static int ResolveLegsPerConfrontation(
        StructureTieFormatSummaryDto? defaultTieFormat,
        int? numberOfLegs)
    {
        var legs = defaultTieFormat?.NumberOfLegs ?? numberOfLegs ?? 1;
        return legs > 0 ? legs : 1;
    }

    internal static int ConfrontationsToMatchSlots(int confrontationCount, int legsPerConfrontation) =>
        confrontationCount <= 0 ? 0 : confrontationCount * legsPerConfrontation;

    internal static int ResolveProspectiveChampionshipMatchSlots(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        int legsPerConfrontation)
    {
        var places = ChampionshipSwissPhasePlaces.ResolvePlacesN(competition, stage, competitionStages);
        if (places is null or < 2)
        {
            return 0;
        }

        var confrontations = places.Value * (places.Value - 1) / 2;
        return ConfrontationsToMatchSlots(confrontations, legsPerConfrontation);
    }

    internal static int ResolveProductionCapacityMatchSlots(
        StructureMaterializeDto materialize,
        StructureFormatKind? formatKind,
        int legsPerConfrontation,
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages)
    {
        var determined = materialize.DeterminedCount;
        if (determined > 0 && !IsGroupsPoolPlacementIncomplete(materialize))
        {
            if (materialize.CommandCode == OverviewAssembler.ActionMaterializeFromOccupiedSlots)
            {
                if (formatKind == StructureFormatKind.Cup)
                {
                    var canonical = materialize.CanonicalEntryPairCount;
                    if (canonical > 0)
                    {
                        return ConfrontationsToMatchSlots(canonical, legsPerConfrontation);
                    }

                    var rows = materialize.Confrontations.Count;
                    if (rows > 0)
                    {
                        return ConfrontationsToMatchSlots(rows, legsPerConfrontation);
                    }
                }

                var capacityConfrontations = Math.Max(determined, materialize.MaterializedCount);
                return ConfrontationsToMatchSlots(capacityConfrontations, legsPerConfrontation);
            }

            return ConfrontationsToMatchSlots(determined, legsPerConfrontation);
        }

        if (formatKind == StructureFormatKind.Cup
            && materialize.CommandCode == OverviewAssembler.ActionMaterializeFromOccupiedSlots)
        {
            var canonical = materialize.CanonicalEntryPairCount;
            if (canonical > 0)
            {
                return ConfrontationsToMatchSlots(canonical, legsPerConfrontation);
            }

            var rows = materialize.Confrontations.Count;
            return rows > 0 ? ConfrontationsToMatchSlots(rows, legsPerConfrontation) : 0;
        }

        if (IsGroupsPoolPlacementIncomplete(materialize))
        {
            var prospective = materialize.ProspectiveConfrontationCount;
            return prospective > 0
                ? ConfrontationsToMatchSlots(prospective, legsPerConfrontation)
                : 0;
        }

        if (formatKind is StructureFormatKind.Championship)
        {
            return ResolveProspectiveChampionshipMatchSlots(
                competition,
                stage,
                competitionStages,
                legsPerConfrontation);
        }

        return 0;
    }

    internal static bool IsGroupsPoolPlacementIncomplete(StructureMaterializeDto materialize)
    {
        if (materialize.DisabledReason == StructureMaterializeDisabledReasons.GroupsNotReady)
        {
            return true;
        }

        var capacity = materialize.GroupsPlacementCapacityCount;
        var filled = materialize.GroupsPlacementFilledCount;
        return capacity > 0 && filled < capacity;
    }

    internal static bool PhaseRosterBlocksProduction(
        StructureMaterializeDto? materialize,
        StructureFormatKind? formatKind,
        PhaseRosterPresentationKind? phaseRosterPresentation,
        StageStatus status)
    {
        if (materialize?.DisabledReason == StructureMaterializeDisabledReasons.PhaseRosterIncomplete)
        {
            return true;
        }

        if (status != StageStatus.Ready
            || formatKind is not (StructureFormatKind.Championship or StructureFormatKind.Swiss))
        {
            return false;
        }

        return phaseRosterPresentation is PhaseRosterPresentationKind.AwaitingInboundQualification
               or PhaseRosterPresentationKind.CompositionIncomplete;
    }

    internal static (int Filled, int Capacity)? ResolveDeterminedConfrontationGauge(
        StructureMaterializeDto materialize)
    {
        var rows = materialize.Confrontations;
        if (rows.Count == 0)
        {
            return null;
        }

        var filled = rows.Count(row => row.Determined);
        return (filled, rows.Count);
    }
}
