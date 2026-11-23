// -----------------------------------------------------------------------
// <copyright file="RedoAlignedRelease.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Releases aligned placements for Refaire — not subject to last-author or fixed-slot
/// release guards; vacancy for the follow-up launch is projected separately.
/// </summary>
internal static class RedoAlignedRelease
{
    internal static HashSet<string> AlignedSlotKeys(Stage stage, Draw draw)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        if (draw.Kind != DrawResolutionKind.Slot || draw.Resolution.State != DrawResolutionState.Resolved)
        {
            return keys;
        }

        foreach (var placement in draw.Resolution.SlotResults)
        {
            if (IsDirectAssignment(stage, placement.SlotKey) || !SlotAligned(stage, placement))
            {
                continue;
            }

            keys.Add(placement.SlotKey);
        }

        return keys;
    }

    internal static void Execute(
        Stage stage,
        Draw draw,
        IClock clock,
        IReadOnlyList<Match>? stageMatches)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(draw);
        ArgumentNullException.ThrowIfNull(clock);

        var snapshots = MaterializedParticipantSnapshots.FromMatches(stageMatches ?? []);

        switch (draw.Kind)
        {
            case DrawResolutionKind.Slot:
                foreach (var placement in draw.Resolution.SlotResults)
                {
                    if (IsDirectAssignment(stage, placement.SlotKey)
                        || !SlotAligned(stage, placement)
                        || SlotProvenanceBlocksRelease(stage, placement.SlotKey))
                    {
                        continue;
                    }

                    stage.ClearResolvedEntry(placement.SlotKey, clock, snapshots);
                }

                break;
            case DrawResolutionKind.Group:
                var drawn = GroupDrawLaunchRules.DrawnEntries(draw);
                foreach (var placement in draw.Resolution.GroupResults)
                {
                    if (!drawn.Contains(placement.EntryId)
                        || stage.FindGroup(placement.GroupId)?.EntryIds.Contains(placement.EntryId) != true
                        || GroupProvenanceBlocksRelease(stage, placement.GroupId, placement.EntryId))
                    {
                        continue;
                    }

                    stage.RemoveDrawEntryFromGroup(placement.GroupId, placement.EntryId, snapshots);
                }

                break;
            default:
                throw new ApplicationFailureException(
                    $"Refaire release does not support draw kind '{draw.Kind}'.",
                    ApplicationErrorCodes.DrawKindNotSupported);
        }
    }

    private static bool SlotAligned(Stage stage, SlotDrawPlacement placement)
    {
        var slot = stage.FindSlot(placement.SlotKey);
        return slot?.EntryId is { } current && current.Equals(placement.EntryId);
    }

    private static bool IsDirectAssignment(Stage stage, string slotKey) =>
        stage.DirectAssignments.Any(assignment =>
            string.Equals(assignment.SlotKey, slotKey, StringComparison.Ordinal));

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
}
