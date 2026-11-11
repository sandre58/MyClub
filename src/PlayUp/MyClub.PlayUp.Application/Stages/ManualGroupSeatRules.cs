// -----------------------------------------------------------------------
// <copyright file="ManualGroupSeatRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Single evaluation of a Groups seat for manual placement routes and the schematic.
/// </summary>
/// <remarks>
/// Priority is the first refusal the matching route would return.
/// Assign (empty seat): active unapplied group draw, then an applied-draw move of that entry,
/// then the domain assign refusal (composition, same-group success, target-group match,
/// lifecycle, qualification/progression, duplicate, unknown group, capacity),
/// then a place reserved for a pending qualification or progression feed.
/// Clear (occupied seat): active unapplied group draw, then applied-draw protection,
/// then the domain clear refusal (lifecycle, missing membership, qualification/progression,
/// any match on the stage).
/// <see cref="StageSchematicAssembler.GroupSeatEditable"/> with <c>CanClear</c> false covers an
/// occupied manual seat whose clear is refused because the stage already has a match.
/// A lifecycle lock (<c>Stage.StructureLocked</c>) is not one of the six seat modes.
/// </remarks>
internal static class ManualGroupSeatRules
{
    internal readonly record struct Assessment(
        string SeatMode,
        bool CanClear,
        bool Excess,
        bool OutsideComposition);

    internal static bool HasActiveUnappliedGroupDraw(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        return stage.Draws.Any(draw =>
            draw.Kind == DrawResolutionKind.Group
            && draw.Status != DrawStatus.Cancelled
            && !DrawAppliedState.IsApplied(draw, stage));
    }

    internal static bool IsReservedForInboundFeed(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        Group group)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(group);
        if (stage.PlacesPerGroup is not int capacity || capacity < 1)
        {
            return false;
        }

        var free = capacity - group.EntryIds.Count;
        if (free <= 0)
        {
            return false;
        }

        var pending = StageSchematicAssembler.CountGroupPendingFeeds(stage, competitionStages, group.Id);
        return pending > 0 && free <= pending;
    }

    internal static void EnsureAssignAllowed(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        GroupId groupId,
        EntryId entryId,
        IReadOnlyList<MatchParticipantSnapshot>? snapshots)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);
        if (HasActiveUnappliedGroupDraw(stage))
        {
            throw ActiveDraw();
        }

        StructureAppliedDrawGroupPlacementGate.EnsureAssignAllowed(stage, groupId, entryId);
        if (stage.PreviewManualGroupAssignRefusal(groupId, entryId, snapshots) is not null)
        {
            return;
        }

        var group = stage.FindGroup(groupId);
        if (group is not null
            && !group.EntryIds.Contains(entryId)
            && IsReservedForInboundFeed(stage, competitionStages, group))
        {
            throw new ApplicationFailureException(
                "This group place is reserved for a pending qualification or progression feed.",
                ApplicationErrorCodes.GroupSeatReservedByInboundFeed);
        }
    }

    internal static void EnsureClearAllowed(Stage stage, GroupId groupId, EntryId entryId)
    {
        ArgumentNullException.ThrowIfNull(stage);
        if (HasActiveUnappliedGroupDraw(stage))
        {
            throw ActiveDraw();
        }

        StructureAppliedDrawGroupPlacementGate.EnsureClearAllowed(stage, groupId, entryId);
    }

    internal static Assessment Project(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        Group group,
        int index,
        EntryId? occupant,
        IReadOnlyCollection<EntryId> composition,
        IReadOnlyList<MatchParticipantSnapshot> snapshots)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(snapshots);

        var excess = stage.PlacesPerGroup is int capacity && index > capacity;
        var outsideComposition = occupant is { } placed && !composition.Contains(placed);
        if (HasActiveUnappliedGroupDraw(stage))
        {
            return new Assessment(StageSchematicAssembler.GroupSeatDrawPending, false, excess, outsideComposition);
        }

        if (occupant is { } entryId)
        {
            if (StructureAppliedDrawGroupPlacementGate.ClearWouldBreakAppliedDraw(stage, group.Id, entryId))
            {
                return new Assessment(StageSchematicAssembler.GroupSeatDraw, false, excess, outsideComposition);
            }

            var clearRefusal = stage.PreviewManualGroupClearRefusal(group.Id, entryId);
            if (clearRefusal == StageErrorCodes.GroupPlacementResolutionLocked)
            {
                return new Assessment(StageSchematicAssembler.GroupSeatQualProg, false, excess, outsideComposition);
            }

            return new Assessment(
                StageSchematicAssembler.GroupSeatEditable,
                clearRefusal is null,
                excess,
                outsideComposition);
        }

        if (stage.ManualGroupAssignBlockedByAttachedMatch(group.Id, snapshots))
        {
            return new Assessment(StageSchematicAssembler.GroupSeatMatchLocked, false, excess, outsideComposition);
        }

        // Lifecycle lock is checked before the reserved-feed guard, and it is not a seat mode.
        if (stage.Status is not (StageStatus.Draft or StageStatus.Ready))
        {
            return new Assessment(StageSchematicAssembler.GroupSeatEditable, false, excess, outsideComposition);
        }

        if (IsReservedForInboundFeed(stage, competitionStages, group))
        {
            return new Assessment(StageSchematicAssembler.GroupSeatReserved, false, excess, outsideComposition);
        }

        return new Assessment(StageSchematicAssembler.GroupSeatEditable, false, excess, outsideComposition);
    }

    private static ApplicationFailureException ActiveDraw() =>
        new(
            "Cancel the draw in progress or in draft before placing or removing a team.",
            ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);
}
