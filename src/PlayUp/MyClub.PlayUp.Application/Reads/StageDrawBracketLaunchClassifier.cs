// -----------------------------------------------------------------------
// <copyright file="StageDrawBracketLaunchClassifier.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Classifies bracket places for Tirage Rerun launch and overview facts (single Application rule).
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
    public static StageDrawBracketLaunchFactsDto ClassifyForDrawKind(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        DrawResolutionKind kind)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);

        return kind switch
        {
            DrawResolutionKind.Slot => ClassifySlots(stage, competitionStages),
            DrawResolutionKind.Group => ClassifyGroups(stage),
            _ => throw new ApplicationFailureException(
                $"Draw bracket launch classification does not support kind '{kind}'.",
                ApplicationErrorCodes.DrawKindNotSupported)
        };
    }

    private static StageDrawBracketLaunchFactsDto ClassifySlots(
        Stage stage,
        IReadOnlyList<Stage> competitionStages)
    {
        var pool = stage.CompositionEntries.Select(entry => entry.EntryId).ToHashSet();
        var snapshot = SlotFeedSnapshotAssembler.Assemble(stage, competitionStages);

        var occupiedByEntry = 0;
        var reservedByFeed = 0;
        var feedInvalid = 0;
        var vacant = 0;

        foreach (var slot in stage.Slots)
        {
            if (slot.EntryId is { } entryId && pool.Contains(entryId))
            {
                occupiedByEntry++;
                continue;
            }

            var resolution = SlotFeedResolver.Resolve(snapshot, slot.SlotKey);
            switch (resolution.Status)
            {
                case FeedResolutionStatus.Missing:
                    vacant++;
                    break;
                case FeedResolutionStatus.Unique:
                    reservedByFeed++;
                    break;
                case FeedResolutionStatus.MultipleFeeds:
                case FeedResolutionStatus.InvalidFeed:
                default:
                    feedInvalid++;
                    break;
            }
        }

        return BuildFacts(
            stage.Slots.Count,
            occupiedByEntry,
            reservedByFeed,
            feedInvalid,
            vacant);
    }

    private static StageDrawBracketLaunchFactsDto ClassifyGroups(Stage stage)
    {
        var pool = stage.CompositionEntries.Select(entry => entry.EntryId).ToHashSet();
        var occupiedByEntry = stage.Groups.Sum(group =>
            group.EntryIds.Count(pool.Contains));

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
