// -----------------------------------------------------------------------
// <copyright file="ManualGroupSeatRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Single evaluation of a Groups seat for the manual routes and the schematic.
/// </summary>
/// <remarks>
/// <see cref="EvaluateAssign"/> and <see cref="EvaluateClear"/> return the first refusal code.
/// <see cref="Project"/> only maps that code to a seat mode.
/// Order: lifecycle (<see cref="StageErrorCodes.StructureLocked"/>), then an unapplied group draw,
/// then applied-draw protection, then the domain refusal, then a place reserved for an unresolved feed.
/// An empty seat has no entry yet. The probe is the first composition entry that is not in any group.
/// When every composition entry is already placed, entry-specific checks are skipped and the seat
/// evaluation covers the lifecycle, the active draw, the target group's match, capacity, and the reserved feed.
/// </remarks>
internal static class ManualGroupSeatRules
{
    internal readonly record struct Assessment(
        string SeatMode,
        bool CanClear,
        bool Excess,
        bool OutsideComposition);

    internal static EntryId? ProbeUnplacedCompositionEntry(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        foreach (var entry in stage.CompositionEntries)
        {
            if (stage.Groups.All(group => !group.EntryIds.Contains(entry.EntryId)))
            {
                return entry.EntryId;
            }
        }

        return null;
    }

    internal static string? EvaluateAssign(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        Group group,
        EntryId? entryId,
        IReadOnlyList<MatchParticipantSnapshot>? snapshots,
        int? unresolvedPendingFeeds = null,
        int? projectedMemberCount = null,
        bool deferCapacityChecks = false)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(group);
        if (stage.FindManualPlacementLifecycleRefusal() is { } lifecycle)
        {
            return lifecycle;
        }

        if (HasActiveUnappliedGroupDraw(stage))
        {
            return ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw;
        }

        if (entryId is { } known)
        {
            if (StructureAppliedDrawGroupPlacementGate.AssignWouldBreakAppliedDraw(stage, group.Id, known))
            {
                return ApplicationErrorCodes.AppliedDrawGroupPlacementProtected;
            }

            if (stage.FindManualAssignRefusal(
                    group.Id,
                    known,
                    snapshots,
                    deferCapacityChecks ? null : projectedMemberCount,
                    ignoreCapacity: deferCapacityChecks) is { } domain)
            {
                return domain;
            }

            if (group.EntryIds.Contains(known))
            {
                return null;
            }
        }
        else if (stage.FindManualAssignSeatRefusal(group.Id, snapshots) is { } seat)
        {
            return seat;
        }

        if (deferCapacityChecks)
        {
            return null;
        }

        var pending = unresolvedPendingFeeds
            ?? GroupPendingFeeds.CountUnresolved(stage, competitionStages, group.Id);
        return IsReserved(stage, group, pending, projectedMemberCount)
            ? ApplicationErrorCodes.GroupSeatReservedByInboundFeed
            : null;
    }

    /// <summary>
    /// First refusal of a seat replacement, or <see langword="null"/> when it would succeed.
    /// Capacity and the inbound-feed reserve do not apply. A replay whose occupant is already
    /// gone and whose replacement is already in the group succeeds only after the lifecycle
    /// and active-draw checks. The same team still runs every guard.
    /// </summary>
    internal static string? EvaluateReplace(
        Stage stage,
        GroupId groupId,
        EntryId currentEntryId,
        EntryId replacementEntryId,
        IReadOnlyList<MatchParticipantSnapshot>? snapshots = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        if (stage.FindManualPlacementLifecycleRefusal() is { } lifecycle)
        {
            return lifecycle;
        }

        if (HasActiveUnappliedGroupDraw(stage))
        {
            return ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw;
        }

        var group = stage.FindGroup(groupId);
        if (group?.EntryIds.Contains(currentEntryId) == false
            && group.EntryIds.Contains(replacementEntryId))
        {
            return null;
        }

        if (StructureAppliedDrawGroupPlacementGate.ClearWouldBreakAppliedDraw(stage, groupId, currentEntryId) || StructureAppliedDrawGroupPlacementGate.AssignWouldBreakAppliedDraw(
                stage,
                groupId,
                replacementEntryId))
        {
            return ApplicationErrorCodes.AppliedDrawGroupPlacementProtected;
        }

        if (stage.FindManualClearRefusal(groupId, currentEntryId) is { } clear)
        {
            return clear;
        }

        if (stage.FindManualAssignRefusal(
                groupId,
                replacementEntryId,
                snapshots,
                ignoreCapacity: true) is { } incoming)
        {
            return incoming;
        }

        if (group?.EntryIds.Contains(replacementEntryId) == true
            && !replacementEntryId.Equals(currentEntryId))
        {
            return StageErrorCodes.DuplicateEntry;
        }

        return null;
    }

    internal static string? EvaluateClear(Stage stage, GroupId groupId, EntryId entryId)
    {
        ArgumentNullException.ThrowIfNull(stage);
        if (stage.FindManualPlacementLifecycleRefusal() is { } lifecycle)
        {
            return lifecycle;
        }

        if (HasActiveUnappliedGroupDraw(stage))
        {
            return ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw;
        }

        return StructureAppliedDrawGroupPlacementGate.ClearWouldBreakAppliedDraw(stage, groupId, entryId) ? ApplicationErrorCodes.AppliedDrawGroupPlacementProtected : stage.FindManualClearRefusal(groupId, entryId);
    }

    internal static Assessment Project(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        Group group,
        int index,
        EntryId? occupant,
        IReadOnlyCollection<EntryId> composition,
        IReadOnlyList<MatchParticipantSnapshot> snapshots,
        int unresolvedPendingFeeds)
    {
        ArgumentNullException.ThrowIfNull(composition);
        var excess = stage.PlacesPerGroup is { } capacity && index > capacity;
        var outsideComposition = occupant is { } placed && !composition.Contains(placed);
        var refusal = occupant is { } entryId
            ? EvaluateClear(stage, group.Id, entryId)
            : EvaluateAssign(
                stage,
                competitionStages,
                group,
                ProbeUnplacedCompositionEntry(stage),
                snapshots,
                unresolvedPendingFeeds);
        var canClear = occupant is not null && refusal is null;
        return new Assessment(GroupSeatModes.FromRefusal(refusal), canClear, excess, outsideComposition);
    }

    internal static void ThrowAssign(Stage stage, GroupId groupId, EntryId entryId, string code)
    {
        if (IsApplicationRefusal(code))
        {
            throw new ApplicationFailureException(ApplicationMessage(code), code);
        }

        throw new DomainException(stage.DescribeManualAssignRefusal(code, groupId, entryId), code);
    }

    internal static void ThrowClear(Stage stage, GroupId groupId, EntryId entryId, string code)
    {
        if (IsApplicationRefusal(code))
        {
            throw new ApplicationFailureException(ApplicationMessage(code), code);
        }

        throw new DomainException(stage.DescribeManualClearRefusal(code, groupId, entryId), code);
    }

    internal static void ThrowReplace(
        Stage stage,
        GroupId groupId,
        EntryId currentEntryId,
        EntryId replacementEntryId,
        string code)
    {
        if (IsApplicationRefusal(code))
        {
            throw new ApplicationFailureException(ApplicationMessage(code), code);
        }

        var clear = stage.FindManualClearRefusal(groupId, currentEntryId);
        var message = clear == code
            ? stage.DescribeManualClearRefusal(code, groupId, currentEntryId)
            : stage.DescribeManualAssignRefusal(code, groupId, replacementEntryId);
        throw new DomainException(message, code);
    }

    internal static void ThrowBatch(Stage stage, GroupId groupId, EntryId entryId, string code)
    {
        var message = IsApplicationRefusal(code)
            ? ApplicationMessage(code)
            : stage.DescribeManualAssignRefusal(code, groupId, entryId);
        throw new GroupPlacementRejectedException(message, code, entryId);
    }

    private static bool IsApplicationRefusal(string code) =>
        code is ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw
            or ApplicationErrorCodes.GroupSeatReservedByInboundFeed
            or ApplicationErrorCodes.AppliedDrawGroupPlacementProtected;

    private static bool HasActiveUnappliedGroupDraw(Stage stage) =>
        stage.Draws.Any(draw =>
            draw.Kind == DrawResolutionKind.Group
            && draw.Status != DrawStatus.Cancelled
            && !DrawAppliedState.IsApplied(draw, stage));

    private static bool IsReserved(Stage stage, Group group, int pending, int? projectedMemberCount = null)
    {
        if (stage.PlacesPerGroup is not { } capacity || capacity < 1 || pending <= 0)
        {
            return false;
        }

        var count = projectedMemberCount ?? group.EntryIds.Count;
        var free = capacity - count;
        return free > 0 && free <= pending;
    }

    private static string ApplicationMessage(string code) =>
        code switch
        {
            ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw =>
                "Cancel the draw, including a draw in draft, or apply it only when it can still be applied, before placing or removing a team.",
            ApplicationErrorCodes.GroupSeatReservedByInboundFeed =>
                "This group place is reserved for a pending qualification or progression feed.",
            _ => "Group placement is protected by the applied draw on this stage."
        };
}
