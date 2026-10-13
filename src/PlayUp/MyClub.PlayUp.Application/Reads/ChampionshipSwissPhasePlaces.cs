// -----------------------------------------------------------------------
// <copyright file="ChampionshipSwissPhasePlaces.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Championship / Swiss phase Places N and composition roster (product D0 — phase population).
/// </summary>
internal static class ChampionshipSwissPhasePlaces
{
    /// <summary>
    /// Target Places N for the championship phase (not always all competition actives).
    /// </summary>
    internal static int? ResolvePlacesN(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages)
    {
        var active = CountActiveEntries(competition);
        if (active == 0)
        {
            return null;
        }

        if (StructureStageInboundTopology.IsRootComposition(stage, competitionStages))
        {
            return active;
        }

        var expected = StructurePopulationVolume.ExpectedOccupancy(stage, competitionStages);
        return expected > 0 ? expected : active;
    }

    /// <summary>
    /// Active entries in phase composition (Materialize / Start / Swiss rounds).
    /// </summary>
    internal static IReadOnlyList<EntryId> ResolveCompositionRoster(Competition competition, Stage stage)
    {
        List<EntryId> roster =
        [
            with(stage.CompositionEntries.Count),
            .. from compositionEntry in stage.CompositionEntries
            let entry = competition.Entries.FirstOrDefault(candidate => candidate.Id.Equals(compositionEntry.EntryId))
            where entry?.Status == EntryStatus.Active
            select compositionEntry.EntryId
        ];
        return roster;
    }

    internal static bool IsRosterComplete(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages)
    {
        var places = ResolvePlacesN(competition, stage, competitionStages);
        if (places is null or <= 0)
        {
            return false;
        }

        var roster = ResolveCompositionRoster(competition, stage);
        return roster.Count >= places.Value;
    }

    /// <summary>
    /// Test and Development helper — fills composition until Places N (stable entry order).
    /// </summary>
    public static void FillCompositionToPlacesCapacity(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(clock);

        var format = StructureSkeleton.InferFormat(stage);
        if (format is not (StructureFormatKind.Championship or StructureFormatKind.Swiss))
        {
            return;
        }

        var capacity = ResolvePlacesN(competition, stage, competitionStages);
        if (capacity is null or <= 0)
        {
            return;
        }

        var orderedActives = competition.Entries
            .Where(entry => entry.Status == EntryStatus.Active)
            .OrderBy(entry => entry.Id.Value)
            .Select(entry => entry.Id)
            .ToArray();

        foreach (var entryId in orderedActives)
        {
            if (stage.CompositionEntries.Count >= capacity.Value)
            {
                break;
            }

            if (stage.CompositionEntries.Any(compositionEntry => compositionEntry.EntryId.Equals(entryId)))
            {
                continue;
            }

            stage.AddResolvedPopulationEntry(entryId, clock);
        }
    }

    private static int CountActiveEntries(Competition competition) =>
        competition.Entries.Count(entry => entry.Status == EntryStatus.Active);
}
