// -----------------------------------------------------------------------
// <copyright file="CascadeMatchAlignment.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages.CascadeReplace;

/// <summary>
/// Points attached match sides at the entry a cascade just selected.
/// A null side is filled. A known side that differs is replaced.
/// A finished match is returned to not played before the side changes.
/// </summary>
internal static class CascadeMatchAlignment
{
    public static void AlignSlot(
        Stage stage,
        string slotKey,
        EntryId entryId,
        IReadOnlyList<Match> matches,
        IClock clock,
        ICollection<CascadePlayedMatch> played)
    {
        var byId = matches
            .Where(match => match.StageId.Equals(stage.Id))
            .ToDictionary(match => match.Id);
        foreach (var round in stage.Rounds)
        {
            foreach (var fixture in round.Fixtures)
            {
                var onA = string.Equals(fixture.SlotAKey, slotKey, StringComparison.Ordinal);
                var onB = string.Equals(fixture.SlotBKey, slotKey, StringComparison.Ordinal);
                if (!onA && !onB)
                {
                    continue;
                }

                foreach (var attachment in fixture.Attachments)
                {
                    if (!byId.TryGetValue(attachment.MatchId, out var match))
                    {
                        continue;
                    }

                    AlignSide(match, SideFor(attachment.LegIndex, onA), entryId, clock, played);
                }
            }
        }
    }

    public static void AlignEntry(
        Stage stage,
        EntryId previous,
        EntryId next,
        IReadOnlyList<Match> matches,
        IClock clock,
        ICollection<CascadePlayedMatch> played)
    {
        foreach (var match in matches.Where(match => match.StageId.Equals(stage.Id)))
        {
            if (match.HomeEntryId is { } home && home.Equals(previous) && !home.Equals(next))
            {
                AlignSide(match, Side.Home, next, clock, played);
            }

            if (match.AwayEntryId is { } away && away.Equals(previous) && !away.Equals(next))
            {
                AlignSide(match, Side.Away, next, clock, played);
            }
        }
    }

    private static void AlignSide(
        Match match,
        Side side,
        EntryId entryId,
        IClock clock,
        ICollection<CascadePlayedMatch> played)
    {
        if (match.Status == MatchStatus.Live)
        {
            throw new ApplicationFailureException(
                $"Match '{match.Id}' is live and cannot change participant.",
                ApplicationErrorCodes.CascadeReplaceLiveBlocked);
        }

        var current = side == Side.Home ? match.HomeEntryId : match.AwayEntryId;
        if (current is { } known && known.Equals(entryId))
        {
            return;
        }

        if (match.Status == MatchStatus.Finished)
        {
            if (played.All(row => row.MatchId != match.Id.Value))
            {
                played.Add(new CascadePlayedMatch(
                    match.Id.Value,
                    match.StageId.Value,
                    match.HomeEntryId?.Value,
                    match.AwayEntryId?.Value));
            }

            match.InvalidateFinishedResult(clock);
        }

        if (current is null)
        {
            match.ResolveParticipant(side, entryId, clock);
            return;
        }

        match.ReplaceParticipant(side, entryId, clock);
    }

    /// <summary>
    /// Leg 1 keeps SlotA as home. Leg 2 mirrors home and away.
    /// </summary>
    private static Side SideFor(int legIndex, bool slotA) =>
        legIndex == 2
            ? slotA ? Side.Away : Side.Home
            : slotA ? Side.Home : Side.Away;
}
