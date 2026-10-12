// -----------------------------------------------------------------------
// <copyright file="ReleaseDrawAlignedPlacements.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: release stage occupants that still exactly match a draw's
/// recorded resolution (explicit liberate — not Cancel rollback, not Apply replace).
/// </summary>
/// <remarks>
/// Slot: for each <c>(SlotKey, EntryId)</c> in <see cref="DrawResolution.SlotResults"/>,
/// release via <see cref="Stage.ClearResolvedEntry"/> when aligned (skips DirectAssignment).
/// Group: for each group placement in <see cref="DrawResolution.GroupResults"/>,
/// <see cref="Stage.RemoveEntryFromGroup"/> when the entry is still in that group.
/// Persistence: caller loads the Stage, invokes this use case, then saves once.
/// </remarks>
public static class ReleaseDrawAlignedPlacements
{
    /// <summary>
    /// Releases aligned occupants for <paramref name="drawId"/> on <paramref name="stage"/>.
    /// </summary>
    /// <param name="stage">Stage that owns the draw.</param>
    /// <param name="drawId">Draw whose resolution defines target pairs.</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <param name="stageMatches">Matches attached on the stage (F1 coherence); empty when none.</param>
    /// <returns>How many placements were released vs skipped.</returns>
    public static ReleaseDrawAlignedPlacementsResult Execute(
        Stage stage,
        DrawId drawId,
        IClock clock,
        IReadOnlyList<Match>? stageMatches = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        var draw = stage.GetDraw(drawId);
        EnsureEligible(draw);
        EnsureNoPublishedAppliedDrawOnStage(stage);
        var snapshots = MaterializedParticipantSnapshots.FromMatches(stageMatches ?? []);

        return draw.Kind switch
        {
            DrawResolutionKind.Slot => ReleaseSlotPlacements(stage, draw, clock, snapshots),
            DrawResolutionKind.Group => ReleaseGroupPlacements(stage, draw, snapshots),
            _ => throw new ApplicationFailureException(
                $"ReleaseDrawAlignedPlacements does not support draw kind '{draw.Kind}'.",
                ApplicationErrorCodes.DrawKindNotSupported)
        };
    }

    private static ReleaseDrawAlignedPlacementsResult ReleaseSlotPlacements(
        Stage stage,
        Draw draw,
        IClock clock,
        IReadOnlyList<MatchParticipantSnapshot> snapshots)
    {
        var released = 0;
        var skipped = 0;

        foreach (var placement in draw.Resolution.SlotResults)
        {
            if (stage.DirectAssignments.Any(a =>
                    string.Equals(a.SlotKey, placement.SlotKey, StringComparison.Ordinal)))
            {
                skipped++;
                continue;
            }

            var slot = stage.FindSlot(placement.SlotKey);
            if (slot?.EntryId is not { } current || !current.Equals(placement.EntryId))
            {
                skipped++;
                continue;
            }

            stage.ClearResolvedEntry(placement.SlotKey, clock, snapshots);
            released++;
        }

        return new ReleaseDrawAlignedPlacementsResult(released, skipped);
    }

    private static ReleaseDrawAlignedPlacementsResult ReleaseGroupPlacements(
        Stage stage,
        Draw draw,
        IReadOnlyList<MatchParticipantSnapshot> snapshots)
    {
        var released = 0;
        var skipped = 0;

        foreach (var placement in draw.Resolution.GroupResults)
        {
            var group = stage.FindGroup(placement.GroupId);
            if (group?.EntryIds.Contains(placement.EntryId) != true)
            {
                skipped++;
                continue;
            }

            stage.RemoveEntryFromGroup(placement.GroupId, placement.EntryId, snapshots);
            released++;
        }

        return new ReleaseDrawAlignedPlacementsResult(released, skipped);
    }

    private static void EnsureNoPublishedAppliedDrawOnStage(Stage stage)
    {
        if (stage.Draws.Any(other => other.Status == DrawStatus.Published && DrawAppliedState.IsApplied(other, stage)))
        {
            throw new ApplicationFailureException(
                "Cannot release aligned placements while a published draw is applied on the stage.",
                ApplicationErrorCodes.DrawReleaseFailure);
        }
    }

    private static void EnsureEligible(Draw draw)
    {
        if (draw.Kind is not DrawResolutionKind.Slot and not DrawResolutionKind.Group)
        {
            throw new ApplicationFailureException(
                $"ReleaseDrawAlignedPlacements does not support draw kind '{draw.Kind}'.",
                ApplicationErrorCodes.DrawKindNotSupported);
        }

        if (draw.Resolution.State != DrawResolutionState.Resolved)
        {
            throw new ApplicationFailureException(
                $"Draw '{draw.Id}' must be Resolved to release aligned placements (resolution is '{draw.Resolution.State}').",
                ApplicationErrorCodes.DrawReleaseFailure);
        }
    }
}

/// <summary>
/// Outcome of <see cref="ReleaseDrawAlignedPlacements.Execute"/>.
/// </summary>
/// <param name="ReleasedCount">Placements cleared on the stage.</param>
/// <param name="SkippedCount">Resolution rows left untouched (divergent or already vacant).</param>
public sealed record ReleaseDrawAlignedPlacementsResult(int ReleasedCount, int SkippedCount);
