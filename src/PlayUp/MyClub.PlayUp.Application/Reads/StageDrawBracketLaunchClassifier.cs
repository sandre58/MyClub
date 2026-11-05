// -----------------------------------------------------------------------
// <copyright file="StageDrawBracketLaunchClassifier.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Classifies bracket places for Tirage Rerun launch and overview facts.
/// Slot places use <see cref="SlotPartialDraw.Classify"/>: a withdrawn occupant is occupied, never vacant.
/// </summary>
public static class StageDrawBracketLaunchClassifier
{
    /// <summary>
    /// Classifies when DrawRules are engaged and the stage has slot or group form units.
    /// </summary>
    public static StageDrawBracketLaunchFactsDto? TryClassify(
        Stage stage,
        IReadOnlyList<Stage> competitionStages)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);

        return stage.Regulation.DrawRules is null
            ? null
            : stage.Slots.Count > 0 ? ClassifySlots(stage, competitionStages) : stage.Groups.Count > 0 ? ClassifyGroups(stage) : null;
    }

    /// <summary>
    /// Classifies for a specific draw resolution kind (Rerun guard).
    /// </summary>
    /// <param name="stage">Stage whose places are classified.</param>
    /// <param name="competitionStages">Competition stages used for inbound feed resolution.</param>
    /// <param name="kind">Draw resolution kind.</param>
    /// <param name="slotKeysTreatedAsVacant">Normalized slot keys treated as vacant before feed resolution.</param>
    /// <param name="groupEntriesTreatedAsRemoved">Pool entries omitted from group occupancy.</param>
    public static StageDrawBracketLaunchFactsDto ClassifyForDrawKind(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        DrawResolutionKind kind,
        IReadOnlySet<string>? slotKeysTreatedAsVacant = null,
        IReadOnlySet<EntryId>? groupEntriesTreatedAsRemoved = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);

        return kind switch
        {
            DrawResolutionKind.Slot => ClassifySlots(stage, competitionStages, slotKeysTreatedAsVacant),
            DrawResolutionKind.Group => ClassifyGroups(stage, groupEntriesTreatedAsRemoved),
            _ => throw new ApplicationFailureException(
                $"Draw bracket launch classification does not support kind '{kind}'.",
                ApplicationErrorCodes.DrawKindNotSupported)
        };
    }

    private static StageDrawBracketLaunchFactsDto ClassifySlots(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlySet<string>? slotKeysTreatedAsVacant = null)
    {
        var snapshot = SlotFeedSnapshotAssembler.Assemble(stage, competitionStages);
        var facts = SlotPartialDraw.Classify(stage, snapshot);

        var occupiedByEntry = 0;
        var reservedByFeed = 0;
        var feedInvalid = 0;
        var vacant = 0;

        for (var index = 0; index < stage.Slots.Count; index++)
        {
            var slot = stage.Slots[index];
            if (slotKeysTreatedAsVacant?.Contains(slot.SlotKey) == true)
            {
                // Treated as vacant ahead of feed resolution: a slot the caller is about to free
                // (e.g. Redo's own draw being cancelled) must not count itself — via its own
                // Published resolution feed — as reserving the place it occupies today.
                vacant++;
                continue;
            }

            switch (facts[index].Occupancy)
            {
                case SlotDrawPlaceOccupancy.Free:
                    vacant++;
                    break;
                case SlotDrawPlaceOccupancy.AlreadyPlaced:
                case SlotDrawPlaceOccupancy.Withdrawn:
                    occupiedByEntry++;
                    break;
                case SlotDrawPlaceOccupancy.StructuralFeed:
                    var status = SlotFeedResolver.Resolve(snapshot, slot.SlotKey).Status;
                    if (status is FeedResolutionStatus.MultipleFeeds or FeedResolutionStatus.InvalidFeed)
                    {
                        feedInvalid++;
                    }
                    else
                    {
                        reservedByFeed++;
                    }

                    break;
                default:
                    throw new InvalidOperationException(
                        $"Slot occupancy '{facts[index].Occupancy}' is not a launch class.");
            }
        }

        return BuildFacts(
            stage.Slots.Count,
            occupiedByEntry,
            reservedByFeed,
            feedInvalid,
            vacant);
    }

    private static StageDrawBracketLaunchFactsDto ClassifyGroups(
        Stage stage,
        IReadOnlySet<EntryId>? groupEntriesTreatedAsRemoved = null)
    {
        var pool = stage.CompositionEntries.Select(entry => entry.EntryId).ToHashSet();
        var occupiedByEntry = stage.Groups.Sum(group =>
            group.EntryIds.Count(entryId =>
                pool.Contains(entryId) && groupEntriesTreatedAsRemoved?.Contains(entryId) != true));

        var entryPlaceCount = pool.Count;
        var vacant = entryPlaceCount - occupiedByEntry;

        return BuildFacts(
            entryPlaceCount,
            occupiedByEntry,
            reservedByStructuralFeed: 0,
            feedInvalid: 0,
            vacant);
    }

    private static StageDrawBracketLaunchFactsDto BuildFacts(
        int entryPlaceCount,
        int occupiedByEntry,
        int reservedByStructuralFeed,
        int feedInvalid,
        int vacant)
    {
        var blocking = occupiedByEntry + reservedByStructuralFeed + feedInvalid;
        return blocking + vacant != entryPlaceCount ? throw new InvalidOperationException("Bracket launch facts must partition entry places (blocking + vacant = entryPlaceCount).") : new StageDrawBracketLaunchFactsDto(entryPlaceCount, occupiedByEntry, reservedByStructuralFeed, feedInvalid, blocking, vacant);
    }
}
