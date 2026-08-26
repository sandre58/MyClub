// -----------------------------------------------------------------------
// <copyright file="DrawAppliedState.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Application-derived draw “Applied” state (Publish ≠ Apply).
/// </summary>
/// <remarks>
/// Domain has no Applied status. Applied means resolution occupancy/attachments already match
/// the published resolution — same heuristic previously reconstructed in the SPA.
/// Pairing: each fixture must have attachments equal to the hosting Round's TieFormat legs
/// (null TieFormat ⇒ 1).
/// </remarks>
public static class DrawAppliedState
{
    /// <summary>
    /// Returns whether a resolved draw appears already applied on the stage.
    /// </summary>
    /// <param name="draw">Draw entity.</param>
    /// <param name="stage">Owning stage.</param>
    /// <returns><see langword="true"/> when placements/attachments match the resolution.</returns>
    public static bool IsApplied(Draw draw, Stage stage)
    {
        ArgumentNullException.ThrowIfNull(draw);
        ArgumentNullException.ThrowIfNull(stage);

        return draw.Resolution.State == DrawResolutionState.Resolved && draw.Kind switch
        {
            DrawResolutionKind.Slot => IsSlotApplied(draw, stage),
            DrawResolutionKind.Pairing => IsPairingApplied(draw, stage),
            _ => false
        };
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
                   var expectedLegs = round.TieFormat?.NumberOfLegs ?? TieFormat.SingleLeg;
                   return fixture.Attachments.Count == expectedLegs;
               });
    }
}
