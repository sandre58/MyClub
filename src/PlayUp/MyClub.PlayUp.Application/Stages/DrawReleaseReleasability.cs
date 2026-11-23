// -----------------------------------------------------------------------
// <copyright file="DrawReleaseReleasability.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Single indexed pass over <see cref="Stage.Draws"/> (list order, Draft skipped) mapping each
/// drawn target to the last cancelled-or-published draw that still aligns on that target.
/// </summary>
internal static class DrawReleaseReleasability
{
    internal sealed class LastDrawnAuthorIndex
    {
        internal LastDrawnAuthorIndex(
            IReadOnlyDictionary<string, DrawId> slotLastAuthorByKey,
            IReadOnlyDictionary<GroupTarget, DrawId> groupLastAuthorByTarget)
        {
            SlotLastAuthorByKey = slotLastAuthorByKey;
            GroupLastAuthorByTarget = groupLastAuthorByTarget;
        }

        internal IReadOnlyDictionary<string, DrawId> SlotLastAuthorByKey { get; }

        internal IReadOnlyDictionary<GroupTarget, DrawId> GroupLastAuthorByTarget { get; }
    }

    internal readonly record struct GroupTarget(GroupId GroupId, EntryId EntryId);

    internal static LastDrawnAuthorIndex BuildIndex(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        var slotAuthors = new Dictionary<string, DrawId>(StringComparer.Ordinal);
        var groupAuthors = new Dictionary<GroupTarget, DrawId>();

        foreach (var draw in stage.Draws)
        {
            if (draw.Status == DrawStatus.Draft
                || draw.Resolution.State != DrawResolutionState.Resolved)
            {
                continue;
            }

            switch (draw.Kind)
            {
                case DrawResolutionKind.Slot:
                    IndexSlotDraw(stage, draw, slotAuthors);
                    break;
                case DrawResolutionKind.Group:
                    IndexGroupDraw(stage, draw, groupAuthors);
                    break;
            }
        }

        return new LastDrawnAuthorIndex(slotAuthors, groupAuthors);
    }

    internal static int CountReleasable(Stage stage, Draw draw, LastDrawnAuthorIndex index)
    {
        if (draw.Resolution.State != DrawResolutionState.Resolved)
        {
            return 0;
        }

        return draw.Kind switch
        {
            DrawResolutionKind.Slot => DrawnSlotPlacements(draw)
                .Count(placement => IsReleasableSlot(stage, draw, placement, index)),
            DrawResolutionKind.Group => draw.Resolution.GroupResults
                .Count(placement => IsReleasableGroup(stage, draw, placement, index)),
            _ => 0
        };
    }

    /// <summary>
    /// Drawn targets in this execution's resolution (slot results minus fixed slots, or drawn group entries).
    /// </summary>
    internal static int CountDrawnTargets(Draw draw)
    {
        if (draw.Resolution.State != DrawResolutionState.Resolved)
        {
            return 0;
        }

        return draw.Kind switch
        {
            DrawResolutionKind.Slot => DrawnSlotPlacements(draw).Count(),
            DrawResolutionKind.Group => GroupDrawLaunchRules.DrawnEntries(draw).Count,
            _ => 0
        };
    }

    internal static bool IsReleasableSlot(
        Stage stage,
        Draw draw,
        SlotDrawPlacement placement,
        LastDrawnAuthorIndex index)
    {
        if (IsDirectAssignment(stage, placement.SlotKey)
            || !SlotAligned(stage, placement)
            || SlotProvenanceBlocksRelease(stage, placement.SlotKey)
            || IsFixedSlot(draw, placement.SlotKey))
        {
            return false;
        }

        return index.SlotLastAuthorByKey.TryGetValue(placement.SlotKey, out var author)
               && author.Equals(draw.Id);
    }

    internal static bool IsReleasableGroup(
        Stage stage,
        Draw draw,
        GroupDrawPlacement placement,
        LastDrawnAuthorIndex index)
    {
        var drawn = GroupDrawLaunchRules.DrawnEntries(draw);
        if (!drawn.Contains(placement.EntryId)
            || stage.FindGroup(placement.GroupId)?.EntryIds.Contains(placement.EntryId) != true
            || GroupProvenanceBlocksRelease(stage, placement.GroupId, placement.EntryId))
        {
            return false;
        }

        var target = new GroupTarget(placement.GroupId, placement.EntryId);
        return index.GroupLastAuthorByTarget.TryGetValue(target, out var author)
               && author.Equals(draw.Id);
    }

    internal static IEnumerable<SlotDrawPlacement> DrawnSlotPlacements(Draw draw)
    {
        var fixedKeys = draw.Inputs?.FixedSlots
            .Select(fixedPlacement => fixedPlacement.SlotKey)
            .ToHashSet(StringComparer.Ordinal) ?? [];

        foreach (var placement in draw.Resolution.SlotResults)
        {
            if (!fixedKeys.Contains(placement.SlotKey))
            {
                yield return placement;
            }
        }
    }

    private static void IndexSlotDraw(
        Stage stage,
        Draw draw,
        Dictionary<string, DrawId> slotAuthors)
    {
        foreach (var placement in DrawnSlotPlacements(draw))
        {
            if (IsDirectAssignment(stage, placement.SlotKey)
                || !SlotAligned(stage, placement)
                || SlotProvenanceBlocksRelease(stage, placement.SlotKey))
            {
                continue;
            }

            slotAuthors[placement.SlotKey] = draw.Id;
        }
    }

    private static void IndexGroupDraw(
        Stage stage,
        Draw draw,
        Dictionary<GroupTarget, DrawId> groupAuthors)
    {
        var drawn = GroupDrawLaunchRules.DrawnEntries(draw);
        foreach (var placement in draw.Resolution.GroupResults)
        {
            if (!drawn.Contains(placement.EntryId)
                || stage.FindGroup(placement.GroupId)?.EntryIds.Contains(placement.EntryId) != true
                || GroupProvenanceBlocksRelease(stage, placement.GroupId, placement.EntryId))
            {
                continue;
            }

            groupAuthors[new GroupTarget(placement.GroupId, placement.EntryId)] = draw.Id;
        }
    }

    private static bool IsFixedSlot(Draw draw, string slotKey) =>
        draw.Inputs?.FixedSlots.Any(fixedPlacement =>
            string.Equals(fixedPlacement.SlotKey, slotKey, StringComparison.Ordinal)) == true;

    private static bool SlotProvenanceBlocksRelease(Stage stage, string slotKey)
    {
        var kind = stage.FindSlot(slotKey)?.ResolutionProvenanceKind;
        return kind is ResolutionProvenanceKind.Qualification
            or ResolutionProvenanceKind.Progression
            or ResolutionProvenanceKind.Directe;
    }

    private static bool GroupProvenanceBlocksRelease(Stage stage, GroupId groupId, EntryId entryId) =>
        stage.GroupResolutionProvenances.Any(row =>
            row.GroupId.Equals(groupId)
            && row.EntryId.Equals(entryId)
            && row.Provenance.Kind is ResolutionProvenanceKind.Qualification
                or ResolutionProvenanceKind.Progression
                or ResolutionProvenanceKind.Directe);

    private static bool SlotAligned(Stage stage, SlotDrawPlacement placement)
    {
        var slot = stage.FindSlot(placement.SlotKey);
        return slot?.EntryId is { } current && current.Equals(placement.EntryId);
    }

    private static bool IsDirectAssignment(Stage stage, string slotKey) =>
        stage.DirectAssignments.Any(assignment =>
            string.Equals(assignment.SlotKey, slotKey, StringComparison.Ordinal));
}
