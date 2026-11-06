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
        ArgumentNullException.ThrowIfNull(stage);

        if (stage.Groups.Count == 0 || stage.PlacesPerGroup is not { } placesPerGroup || placesPerGroup < 1)
        {
            return (0, 0);
        }

        var composition = stage.CompositionEntries.Select(entry => entry.EntryId).ToHashSet();
        var capacity = stage.Groups.Count * placesPerGroup;
        var filled = stage.Groups.Sum(group => CountValidGroupMembers(group, composition));
        return (filled, capacity);
    }

    /// <summary>
    /// True when pools are not full enough for match production or Start (minimum two per pool when capacity unknown).
    /// </summary>
    internal static bool BlocksProduction(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (stage.Groups.Count == 0)
        {
            return true;
        }

        var composition = stage.CompositionEntries.Select(entry => entry.EntryId).ToHashSet();

        if (stage.PlacesPerGroup is not { } placesPerGroup || placesPerGroup < 1)
        {
            return stage.Groups.Any(group => CountValidGroupMembers(group, composition) < 2);
        }

        return stage.Groups.Any(group =>
        {
            var valid = CountValidGroupMembers(group, composition);
            return valid < 2 || valid != placesPerGroup;
        });
    }

    private static int CountValidGroupMembers(Group group, HashSet<EntryId> composition) =>
        group.EntryIds.Count(entryId => composition.Contains(entryId));
}
