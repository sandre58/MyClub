// -----------------------------------------------------------------------
// <copyright file="MaterializedOpponentCoherence.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Domain rule: mutation of a determination source after materialization must stay coherent
/// with attached match opponents (D1–D4 / F1). Not a Confrontation aggregate.
/// </summary>
/// <remarks>
/// Cup: BracketPair slot occupants vs attached Fixture matches (final simulated pair state).
/// Groups: remove/move of an entry that appears on an attached match is refused; Add is allowed.
/// </remarks>
public static class MaterializedOpponentCoherence
{
    /// <summary>
    /// Copies current slot occupants (normalized keys).
    /// </summary>
    public static Dictionary<string, EntryId?> CopySlotOccupants(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        var map = new Dictionary<string, EntryId?>(stage.Slots.Count, StringComparer.Ordinal);
        foreach (var slot in stage.Slots)
        {
            map[slot.SlotKey] = slot.EntryId;
        }

        return map;
    }

    /// <summary>
    /// Ensures every BracketPair that already has attached matches stays coherent with
    /// <paramref name="slotOccupantsAfter"/> (final simulated state).
    /// </summary>
    public static void EnsureCupPairsCoherent(
        Stage stage,
        IReadOnlyDictionary<string, EntryId?> slotOccupantsAfter,
        IReadOnlyList<MatchParticipantSnapshot> materializedParticipants)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(slotOccupantsAfter);
        ArgumentNullException.ThrowIfNull(materializedParticipants);

        if (stage.BracketPairs.Count == 0)
        {
            return;
        }

        var byMatchId = IndexSnapshots(materializedParticipants);
        var current = CopySlotOccupants(stage);

        foreach (var pair in stage.BracketPairs)
        {
            current.TryGetValue(pair.SlotAKey, out var beforeA);
            current.TryGetValue(pair.SlotBKey, out var beforeB);
            slotOccupantsAfter.TryGetValue(pair.SlotAKey, out var occA);
            slotOccupantsAfter.TryGetValue(pair.SlotBKey, out var occB);

            var pairImpacted = !Nullable.Equals(beforeA, occA) || !Nullable.Equals(beforeB, occB);
            if (!pairImpacted)
            {
                continue;
            }

            var fixture = stage.FindFixtureByBracketPairKey(pair.PairKey);
            if (fixture is null || fixture.Attachments.Count == 0)
            {
                continue;
            }

            var opponents = ResolveAttachedOpponents(fixture, byMatchId);

            if (occA is null || occB is null || occA.Equals(occB))
            {
                throw Conflict(
                    $"Bracket pair '{pair.PairKey}' is materialized; slot occupants cannot diverge from attached match participants.");
            }

            var occupantSet = new HashSet<EntryId> { occA.Value, occB.Value };
            if (!occupantSet.SetEquals(opponents))
            {
                throw Conflict(
                    $"Bracket pair '{pair.PairKey}' is materialized; slot occupants must remain coherent with attached match participants.");
            }
        }
    }

    /// <summary>
    /// Ensures an entry that appears on an attached match cannot leave its determination group (C3 remove/move).
    /// </summary>
    public static void EnsureEntryNotBoundToAttachedMatch(
        Stage stage,
        EntryId entryId,
        IReadOnlyList<MatchParticipantSnapshot> materializedParticipants)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(materializedParticipants);

        if (stage.Groups.Count == 0)
        {
            return;
        }

        var attached = CollectAttachedMatchIds(stage);
        if (attached.Count == 0)
        {
            return;
        }

        foreach (var snapshot in materializedParticipants)
        {
            if (!attached.Contains(snapshot.MatchId))
            {
                continue;
            }

            if (snapshot.HomeEntryId.Equals(entryId) || snapshot.AwayEntryId.Equals(entryId))
            {
                throw Conflict(
                    $"Entry '{entryId}' appears on materialized match '{snapshot.MatchId}' and cannot leave its group.");
            }
        }
    }

    private static Dictionary<MatchId, MatchParticipantSnapshot> IndexSnapshots(
        IReadOnlyList<MatchParticipantSnapshot> materializedParticipants)
    {
        var byMatchId = new Dictionary<MatchId, MatchParticipantSnapshot>(materializedParticipants.Count);
        foreach (var snapshot in materializedParticipants)
        {
            byMatchId[snapshot.MatchId] = snapshot;
        }

        return byMatchId;
    }

    private static HashSet<EntryId> ResolveAttachedOpponents(
        Fixture fixture,
        Dictionary<MatchId, MatchParticipantSnapshot> byMatchId)
    {
        var opponents = new HashSet<EntryId>();
        foreach (var attachment in fixture.Attachments)
        {
            if (!byMatchId.TryGetValue(attachment.MatchId, out var snapshot))
            {
                throw Conflict(
                    $"Materialized match '{attachment.MatchId}' on fixture '{fixture.Id}' was not provided in participant snapshots.");
            }

            opponents.Add(snapshot.HomeEntryId);
            opponents.Add(snapshot.AwayEntryId);
        }

        return opponents;
    }

    private static HashSet<MatchId> CollectAttachedMatchIds(Stage stage)
    {
        var attached = new HashSet<MatchId>();
        foreach (var matchday in stage.Matchdays)
        {
            foreach (var fixture in matchday.Fixtures)
            {
                foreach (var attachment in fixture.Attachments)
                {
                    attached.Add(attachment.MatchId);
                }
            }
        }

        foreach (var round in stage.Rounds)
        {
            foreach (var fixture in round.Fixtures)
            {
                foreach (var attachment in fixture.Attachments)
                {
                    attached.Add(attachment.MatchId);
                }
            }
        }

        return attached;
    }

    private static DomainException Conflict(string message) =>
        new(message, StageErrorCodes.MaterializedOpponentConflict);
}
