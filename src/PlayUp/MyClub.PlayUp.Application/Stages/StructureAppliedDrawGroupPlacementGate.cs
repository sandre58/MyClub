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
        return (from draw in stage.Draws where DrawAppliedState.IsApplied(draw, stage) && draw.Kind == DrawResolutionKind.Group from placement in draw.Resolution.GroupResults where placement.EntryId.Equals(proposedEntryId) select !placement.GroupId.Equals(groupId)).FirstOrDefault();
    }

    internal static bool ClearWouldBreakAppliedDraw(Stage stage, GroupId groupId, EntryId entryId)
    {
        ArgumentNullException.ThrowIfNull(stage);
        return stage.Draws.Any(draw => DrawAppliedState.IsApplied(draw, stage) && draw.Kind == DrawResolutionKind.Group && draw.Resolution.GroupResults.Any(placement => placement.GroupId.Equals(groupId) && placement.EntryId.Equals(entryId)));
    }

    private static void ThrowProtected() =>
        throw new ApplicationFailureException(
            BlockMessage,
            ApplicationErrorCodes.AppliedDrawGroupPlacementProtected);
}
