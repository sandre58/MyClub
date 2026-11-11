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
        int? unresolvedPendingFeeds = null)
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

            if (stage.FindManualAssignRefusal(group.Id, known, snapshots) is { } domain)
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

        var pending = unresolvedPendingFeeds
            ?? GroupPendingFeeds.CountUnresolved(stage, competitionStages, group.Id);
        return IsReserved(stage, group, pending)
            ? ApplicationErrorCodes.GroupSeatReservedByInboundFeed
            : null;
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

        if (StructureAppliedDrawGroupPlacementGate.ClearWouldBreakAppliedDraw(stage, groupId, entryId))
        {
            return ApplicationErrorCodes.AppliedDrawGroupPlacementProtected;
        }

        return stage.FindManualClearRefusal(groupId, entryId);
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
        var excess = stage.PlacesPerGroup is int capacity && index > capacity;
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

    private static bool IsApplicationRefusal(string code) =>
        code is ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw
            or ApplicationErrorCodes.GroupSeatReservedByInboundFeed
            or ApplicationErrorCodes.AppliedDrawGroupPlacementProtected;

    private static bool HasActiveUnappliedGroupDraw(Stage stage) =>
        stage.Draws.Any(draw =>
            draw.Kind == DrawResolutionKind.Group
            && draw.Status != DrawStatus.Cancelled
            && !DrawAppliedState.IsApplied(draw, stage));

    private static bool IsReserved(Stage stage, Group group, int pending)
    {
        if (stage.PlacesPerGroup is not int capacity || capacity < 1 || pending <= 0)
        {
            return false;
        }

        var free = capacity - group.EntryIds.Count;
        return free > 0 && free <= pending;
    }

    private static string ApplicationMessage(string code) =>
        code switch
        {
            ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw =>
                "Cancel the draw, including a draw in draft, or apply it only when it can still be applied, before placing or removing a team.",
            ApplicationErrorCodes.GroupSeatReservedByInboundFeed =>
                "This group place is reserved for a pending qualification or progression feed.",
            _ => "Group placement is protected by the applied draw on this stage.",
        };
}
