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
/// Refuses manual group membership mutations that break placements guaranteed by an Applied group draw.
/// </summary>
internal static class StructureAppliedDrawGroupPlacementGate
{
    private const string BlockMessage =
        "Group placement is protected by the applied draw on this stage.";

    internal static void EnsureAssignAllowed(Stage stage, GroupId groupId, EntryId proposedEntryId)
    {
        if (AssignWouldBreakAppliedDraw(stage, groupId, proposedEntryId))
        {
            ThrowProtected();
        }
    }

    internal static void EnsureClearAllowed(Stage stage, GroupId groupId, EntryId entryId)
    {
        if (ClearWouldBreakAppliedDraw(stage, groupId, entryId))
        {
            ThrowProtected();
        }
    }

    internal static bool AssignWouldBreakAppliedDraw(Stage stage, GroupId groupId, EntryId proposedEntryId)
    {
        ArgumentNullException.ThrowIfNull(stage);
        foreach (var draw in stage.Draws)
        {
            if (!DrawAppliedState.IsApplied(draw, stage) || draw.Kind != DrawResolutionKind.Group)
            {
                continue;
            }

            foreach (var placement in draw.Resolution.GroupResults)
            {
                if (!placement.EntryId.Equals(proposedEntryId))
                {
                    continue;
                }

                return !placement.GroupId.Equals(groupId);
            }
        }

        return false;
    }

    internal static bool ClearWouldBreakAppliedDraw(Stage stage, GroupId groupId, EntryId entryId)
    {
        ArgumentNullException.ThrowIfNull(stage);
        foreach (var draw in stage.Draws)
        {
            if (!DrawAppliedState.IsApplied(draw, stage) || draw.Kind != DrawResolutionKind.Group)
            {
                continue;
            }

            if (draw.Resolution.GroupResults.Any(placement =>
                    placement.GroupId.Equals(groupId) && placement.EntryId.Equals(entryId)))
            {
                return true;
            }
        }

        return false;
    }

    private static void ThrowProtected() =>
        throw new ApplicationFailureException(
            BlockMessage,
            ApplicationErrorCodes.AppliedDrawGroupPlacementProtected);
}
