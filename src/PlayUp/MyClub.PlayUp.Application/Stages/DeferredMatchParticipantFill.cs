// -----------------------------------------------------------------------
// <copyright file="DeferredMatchParticipantFill.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Fills unknown cup-match sides after Apply resolves the source slot.
/// The slot reference stays on the fixture (SlotA / SlotB + leg), not on the match.
/// </summary>
internal static class DeferredMatchParticipantFill
{
    /// <summary>
    /// Resolves every attached match side that still points at <paramref name="slotKey"/>.
    /// </summary>
    public static void FillSlot(
        Stage stage,
        string slotKey,
        EntryId entryId,
        IEnumerable<Match> matches,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(slotKey);
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(clock);

        var byId = matches
            .Where(match => match.StageId.Equals(stage.Id))
            .ToDictionary(match => match.Id);

        foreach (var round in stage.Rounds)
        {
            foreach (var fixture in round.Fixtures)
            {
                FillFixture(fixture, slotKey, entryId, byId, clock);
            }
        }
    }

    private static void FillFixture(
        Fixture fixture,
        string slotKey,
        EntryId entryId,
        Dictionary<MatchId, Match> matches,
        IClock clock)
    {
        var onA = string.Equals(fixture.SlotAKey, slotKey, StringComparison.Ordinal);
        var onB = string.Equals(fixture.SlotBKey, slotKey, StringComparison.Ordinal);
        if (!onA && !onB)
        {
            return;
        }

        foreach (var attachment in fixture.Attachments)
        {
            if (!matches.TryGetValue(attachment.MatchId, out var match))
            {
                continue;
            }

            match.ResolveParticipant(SideFor(attachment.LegIndex, onA), entryId, clock);
        }
    }

    /// <summary>
    /// Leg 1 keeps SlotA as home. Leg 2 mirrors home and away.
    /// </summary>
    private static Side SideFor(int legIndex, bool slotA) =>
        legIndex == 2
            ? slotA ? Side.Away : Side.Home
            : slotA ? Side.Home : Side.Away;
}
