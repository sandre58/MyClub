// -----------------------------------------------------------------------
// <copyright file="ReleaseDrawAlignedPlacements.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: release slot occupants that still exactly match a Slot draw's
/// recorded resolution (decision D — explicit liberate, not Cancel rollback, not Apply replace).
/// </summary>
/// <remarks>
/// For each <c>(SlotKey, EntryId)</c> in <see cref="DrawResolution.SlotResults"/>:
/// release via <see cref="Stage.ClearResolvedEntry"/> only when the current occupant equals
/// that EntryId and the slot has no <see cref="DirectAssignment"/>; otherwise skip.
/// Persistence: caller loads the Stage, invokes this use case, then saves once.
/// </remarks>
public static class ReleaseDrawAlignedPlacements
{
    /// <summary>
    /// Releases aligned slot occupants for <paramref name="drawId"/> on <paramref name="stage"/>.
    /// </summary>
    /// <param name="stage">Stage that owns the draw and slots.</param>
    /// <param name="drawId">Draw whose <see cref="DrawResolution.SlotResults"/> define the target pairs.</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>How many slots were released vs skipped.</returns>
    public static ReleaseDrawAlignedPlacementsResult Execute(
        Stage stage,
        DrawId drawId,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        var draw = stage.GetDraw(drawId);
        EnsureEligible(draw);

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

            stage.ClearResolvedEntry(placement.SlotKey, clock);
            released++;
        }

        return new ReleaseDrawAlignedPlacementsResult(released, skipped);
    }

    private static void EnsureEligible(Draw draw)
    {
        if (draw.Kind != DrawResolutionKind.Slot)
        {
            throw new ApplicationFailureException(
                $"ReleaseDrawAlignedPlacements supports Slot draws only (kind is '{draw.Kind}').",
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
/// <param name="ReleasedCount">Slots cleared via <see cref="Stage.ClearResolvedEntry"/>.</param>
/// <param name="SkippedCount">Placements left untouched (DA, vacant, or divergent occupant).</param>
public sealed record ReleaseDrawAlignedPlacementsResult(int ReleasedCount, int SkippedCount);
