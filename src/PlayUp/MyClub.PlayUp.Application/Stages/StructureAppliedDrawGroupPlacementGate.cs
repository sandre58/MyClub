// -----------------------------------------------------------------------
// <copyright file="StructureAppliedDrawGroupPlacementGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Refuses manual group membership mutations that move a team drawn by an applied group draw.
/// Teams fixed before the draw, including qualification and progression arrivals, are not in this scope.
/// </summary>
internal static class StructureAppliedDrawGroupPlacementGate
{
    internal static bool AssignWouldBreakAppliedDraw(Stage stage, GroupId groupId, EntryId proposedEntryId)
    {
        ArgumentNullException.ThrowIfNull(stage);
        return AppliedDraws(stage).Any(draw =>
            AppliedGroupDrawScope.DrawnPlacements(draw).Any(placement =>
                placement.EntryId.Equals(proposedEntryId) && !placement.GroupId.Equals(groupId)));
    }

    internal static bool ClearWouldBreakAppliedDraw(Stage stage, GroupId groupId, EntryId entryId)
    {
        ArgumentNullException.ThrowIfNull(stage);
        return AppliedDraws(stage).Any(draw =>
            AppliedGroupDrawScope.DrawnPlacements(draw).Any(placement =>
                placement.GroupId.Equals(groupId) && placement.EntryId.Equals(entryId)));
    }

    private static IEnumerable<Draw> AppliedDraws(Stage stage) =>
        stage.Draws.Where(draw =>
            draw.Kind == DrawResolutionKind.Group && DrawAppliedState.IsApplied(draw, stage));
}
