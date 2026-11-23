// -----------------------------------------------------------------------
// <copyright file="AppliedGroupDrawScope.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Drawn group placements of one draw. Callers do not filter <see cref="Draw.Resolution"/> themselves.
/// </summary>
internal static class AppliedGroupDrawScope
{
    /// <summary>
    /// Resolution rows that are not in <see cref="DrawInputs.FixedGroups"/>.
    /// A draw with no inputs treats the whole resolution as drawn.
    /// </summary>
    internal static IReadOnlyList<GroupDrawPlacement> DrawnPlacements(Draw draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        var drawn = GroupDrawLaunchRules.DrawnEntries(draw);
        return
        [
            .. draw.Resolution.GroupResults
                .Where(placement => drawn.Contains(placement.EntryId))
        ];
    }

    /// <summary>
    /// Rows that must still occupy their group for the draw to stay applied.
    /// When the draw has no drawn entry, every resolution row counts. Otherwise only drawn rows count.
    /// </summary>
    internal static IReadOnlyList<GroupDrawPlacement> PlacementsThatKeepApplied(Draw draw)
    {
        var drawn = DrawnPlacements(draw);
        return drawn.Count == 0 ? draw.Resolution.GroupResults : drawn;
    }
}
