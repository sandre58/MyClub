// -----------------------------------------------------------------------
// <copyright file="StageLifecycleTestSupport.cs" company="Stéphane ANDRE">
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
/// Test / Development helper mirroring Host Prepare gates before materialize.
/// </summary>
public static class StageLifecycleTestSupport
{
    /// <summary>
    /// Fills Championship/Swiss composition to Places N, runs Prepare gates, then <see cref="PrepareStage"/>.
    /// </summary>
    public static void EnsureReadyForMaterialize(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(clock);

        FillCompositionForPrepare(competition, stage, competitionStages, clock);

        if (stage.Status is StageStatus.Ready)
        {
            return;
        }

        StructurePreparePlacesGate.Ensure(competition, stage, competitionStages);
        PrepareStage.Execute(stage, competitionStages, clock);
    }

    private static void FillCompositionForPrepare(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        IClock clock)
    {
        var format = StructureSkeleton.InferFormat(stage);
        if (format is StructureFormatKind.Championship or StructureFormatKind.Swiss)
        {
            ChampionshipSwissPhasePlaces.FillCompositionToPlacesCapacity(
                competition,
                stage,
                competitionStages,
                clock);
            return;
        }

        if (format is not StructureFormatKind.Groups)
        {
            return;
        }

        var capacity = StructureViewAssembler.GetCompositionCapacity(competition, stage, competitionStages);
        if (capacity is null or <= 0)
        {
            return;
        }

        foreach (var entryId in stage.Groups.SelectMany(group => group.EntryIds))
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
}
