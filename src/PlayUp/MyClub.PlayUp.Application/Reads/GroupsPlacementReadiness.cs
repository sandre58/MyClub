// -----------------------------------------------------------------------
// <copyright file="GroupsPlacementReadiness.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Groups pool placement vs production / Start (shared by materialize read and <see cref="Stages.StageStartReadinessGuard"/>).
/// </summary>
internal static class GroupsPlacementReadiness
{
    internal static (int Filled, int Capacity) GetPlacementProgress(Stage stage)
    {
        var evaluated = Evaluate(stage);
        return (evaluated.Filled, evaluated.Capacity);
    }

    /// <summary>
    /// True when pools are not full enough for match production or Start (minimum two per pool when capacity unknown).
    /// </summary>
    internal static bool BlocksProduction(Stage stage) => Evaluate(stage).BlocksProduction;

    /// <summary>
    /// Placement progress and the production block, from one composition set.
    /// </summary>
    internal static (int Filled, int Capacity, bool BlocksProduction) Evaluate(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (stage.Groups.Count == 0)
        {
            return (0, 0, true);
        }

        var composition = stage.CompositionEntries.Select(entry => entry.EntryId).ToHashSet();
        var validCounts = stage.Groups
            .Select(group => CountValidGroupMembers(group, composition))
            .ToArray();

        if (stage.PlacesPerGroup is not { } placesPerGroup || placesPerGroup < 1)
        {
            return (0, 0, validCounts.Any(valid => valid < 2));
        }

        var filled = validCounts.Sum(valid => Math.Min(valid, placesPerGroup));
        var capacity = stage.Groups.Count * placesPerGroup;
        return (filled, capacity, validCounts.Any(valid => valid != placesPerGroup));
    }

    private static int CountValidGroupMembers(Group group, HashSet<EntryId> composition) =>
        group.EntryIds.Count(entryId => composition.Contains(entryId));
}
