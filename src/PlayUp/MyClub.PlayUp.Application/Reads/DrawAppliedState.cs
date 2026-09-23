// -----------------------------------------------------------------------
// <copyright file="DrawAppliedState.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Application-derived draw “Applied” state (Publish ≠ Apply).
/// </summary>
/// <remarks>
/// Domain has no Applied status. Applied means a <see cref="DrawStatus.Published"/> draw's
/// resolution occupancy/attachments already match the stage — Publish ≠ Apply.
/// Draft/Cancelled draws never report Applied even if stage occupancy still matches a prior Apply
/// (Cancel does not unwind materialization).
/// Pairing: each fixture must have attachments equal to the hosting Round's TieFormat legs
/// (null TieFormat ⇒ OneLeg).
/// </remarks>
public static class DrawAppliedState
{
    /// <summary>
    /// Returns whether a published resolved draw appears already applied on the stage.
    /// </summary>
    /// <param name="draw">Draw entity.</param>
    /// <param name="stage">Owning stage.</param>
    /// <returns><see langword="true"/> when the draw is Published and placements/attachments match.</returns>
    public static bool IsApplied(Draw draw, Stage stage)
    {
        ArgumentNullException.ThrowIfNull(draw);
        ArgumentNullException.ThrowIfNull(stage);

        if (draw.Status != DrawStatus.Published
            || draw.Resolution.State != DrawResolutionState.Resolved)
        {
            return false;
        }

        return draw.Kind switch
        {
            DrawResolutionKind.Slot => IsSlotApplied(draw, stage),
            DrawResolutionKind.Group => IsGroupApplied(draw, stage),
            DrawResolutionKind.Pairing => IsPairingApplied(draw, stage),
            _ => false
        };
    }

    private static bool IsGroupApplied(Draw draw, Stage stage)
    {
        var placements = draw.Resolution.GroupResults;
        return placements.Count != 0 && !(from placement in placements let @group = stage.FindGroup(placement.GroupId) where @group?.EntryIds.Contains(placement.EntryId) != true select placement).Any();
    }

    private static bool IsSlotApplied(Draw draw, Stage stage)
    {
        var placements = draw.Resolution.SlotResults;
        if (placements.Count == 0)
        {
            return false;
        }

        var byKey = stage.Slots.ToDictionary(slot => slot.SlotKey, StringComparer.Ordinal);
        foreach (var placement in placements)
        {
            if (!byKey.TryGetValue(placement.SlotKey, out var slot)
                || slot.EntryId is null
                || !slot.EntryId.Equals(placement.EntryId))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPairingApplied(Draw draw, Stage stage)
    {
        var pairings = draw.Resolution.PairingResults;
        var fixtures = stage.Rounds.SelectMany(round => round.Fixtures).ToArray();
        return pairings.Count != 0
               && fixtures.Length != 0
               && pairings.Count == fixtures.Length
               && fixtures.All(fixture =>
               {
                   var round = stage.Rounds.First(r => r.Fixtures.Any(f => f.Id.Equals(fixture.Id)));
                   var expectedLegs = TieFormat.OrDefaultOneLeg(round.TieFormat).NumberOfLegs;
                   return fixture.Attachments.Count == expectedLegs;
               });
    }
}
