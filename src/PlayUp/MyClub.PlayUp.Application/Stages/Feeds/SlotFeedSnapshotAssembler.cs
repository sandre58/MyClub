// -----------------------------------------------------------------------
// <copyright file="SlotFeedSnapshotAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Builds a <see cref="SlotFeedSnapshot"/> from already-loaded stages (no repository required).
/// </summary>
public static class SlotFeedSnapshotAssembler
{
    /// <summary>
    /// Assembles inbound and local configuration feeds for <paramref name="target"/>.
    /// </summary>
    /// <param name="target">Destination stage whose slots are resolved.</param>
    /// <param name="competitionStages">All stages of the competition (already loaded).</param>
    /// <returns>An immutable domain snapshot.</returns>
    public static SlotFeedSnapshot Assemble(
        Stage target,
        IReadOnlyList<Stage> competitionStages)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(competitionStages);

        if (competitionStages.All(s => !s.Id.Equals(target.Id)))
        {
            throw new ApplicationFailureException(
                $"Stage '{target.Id}' is not part of the competition stages list.",
                ApplicationErrorCodes.StageNotInCompetition);
        }

        var slotKeys = target.Slots.Select(s => s.SlotKey).ToArray();

        var directs = target.DirectAssignments
            .Select(a => new DirectFeedSource(a.SlotKey, a.EntryId))
            .ToArray();

        var qualifications = Array.Empty<QualificationFeedSource>();
        var progressions = new List<ProgressionFeedSource>();

        // Qualification targets population only (Qual V2) — WhoFeeds stays slot-centric.
        foreach (var stage in competitionStages)
        {
            if (stage.Regulation.ProgressionRules is not { } progressionRules) continue;

            foreach (var path in progressionRules.Paths)
            {
                if (!path.Destination.StageId.Equals(target.Id))
                {
                    continue;
                }

                // Population destinations (O2-a) do not feed slots — WhoFeeds stays slot-centric.
                if (path.Destination.TargetsPopulation)
                {
                    continue;
                }

                EnsureSlotExists(target, path.Destination.SlotKey!, stage.Id, "Progression");
                progressions.Add(
                    new ProgressionFeedSource(
                        stage.Id,
                        path.SourceFixtureId,
                        path.Outcome,
                        path.Destination.SlotKey!));
            }
        }

        return new SlotFeedSnapshot(
            target.Id,
            slotKeys,
            directs,
            qualifications,
            progressions,
            BuildDrawTargets(target));
    }

    /// <summary>
    /// Draw → SlotResolution (Published) → DrawFeedSource → WhoFeeds.
    /// Draft / Cancelled / NoSolution draws do not contribute feeds (Prepare before Publish → Missing).
    /// </summary>
    private static DrawFeedSource[] BuildDrawTargets(Stage target)
    {
        var targets = new List<DrawFeedSource>();
        foreach (var draw in target.Draws)
        {
            if (draw.Status != DrawStatus.Published
                || draw.Kind != DrawResolutionKind.Slot
                || draw.Resolution.State != DrawResolutionState.Resolved)
            {
                continue;
            }

            targets.AddRange(draw.Resolution.SlotResults.Select(placement => new DrawFeedSource(placement.SlotKey, draw.Id)));
        }

        return [..targets];
    }

    private static void EnsureSlotExists(
        Stage target,
        string destinationSlotKey,
        StageId sourceStageId,
        string mechanism)
    {
        if (target.FindSlot(destinationSlotKey) is not null)
        {
            return;
        }

        throw new ApplicationFailureException(
            $"{mechanism} path from stage '{sourceStageId}' targets unknown slot '{destinationSlotKey}'.",
            ApplicationErrorCodes.DanglingFeedTarget);
    }
}
