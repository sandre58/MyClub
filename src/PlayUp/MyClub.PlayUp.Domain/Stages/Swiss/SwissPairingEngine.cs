// -----------------------------------------------------------------------
// <copyright file="SwissPairingEngine.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Pure Domain Swiss round pairing (I5–I8): hard constraints → I7 order → deterministic backtracking.
/// </summary>
/// <remarks>
/// Same inputs always yield the same pairings. No randomization, timeout, or CSP library.
/// Standings / played pairs / bye history are explicit request inputs — never read from infrastructure.
/// </remarks>
public static class SwissPairingEngine
{
    /// <summary>
    /// Builds a complete set of pairings for one Swiss round, or <see cref="SwissPairingResult.NoSolution"/>.
    /// </summary>
    /// <param name="request">Explicit standings, history, and counts.</param>
    /// <returns>Success with pairings (+ optional bye) or NoSolution.</returns>
    public static SwissPairingResult BuildPairings(SwissPairingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var byEntry = request.Standings.ToDictionary(row => row.EntryId);
        var played = BuildPlayedSet(request.PlayedPairs);
        var ordered = request.Standings
            .OrderBy(row => row.Position)
            .ThenBy(row => row.EntryId.Value)
            .Select(row => row.EntryId)
            .ToList();

        if (ordered.Count % 2 == 0)
        {
            var pairings = new List<SwissPairing>(ordered.Count / 2);
            return TryPair(ordered, pairings, byEntry, played, request.HomeCountsByEntry)
                ? SwissPairingResult.Success(pairings)
                : SwissPairingResult.NoSolution();
        }

        foreach (var byeEntry in OrderByeCandidates(ordered, byEntry, request.ByeCountsByEntry))
        {
            var remaining = ordered.Where(id => !id.Equals(byeEntry)).ToList();
            var pairings = new List<SwissPairing>(remaining.Count / 2);
            if (TryPair(remaining, pairings, byEntry, played, request.HomeCountsByEntry))
            {
                return SwissPairingResult.Success(pairings, byeEntry);
            }
        }

        return SwissPairingResult.NoSolution();
    }

    private static HashSet<(Guid Low, Guid High)> BuildPlayedSet(
        IReadOnlyList<(EntryId First, EntryId Second)> playedPairs)
    {
        var set = new HashSet<(Guid Low, Guid High)>();
        foreach (var (first, second) in playedPairs)
        {
            if (first.Equals(second))
            {
                throw new DomainException(
                    "Played pair cannot contain the same entry twice.",
                    StageErrorCodes.SwissPairingInvalid);
            }

            set.Add(Canonical(first, second));
        }

        return set;
    }

    private static (Guid Low, Guid High) Canonical(EntryId left, EntryId right) =>
        left.Value.CompareTo(right.Value) <= 0
            ? (left.Value, right.Value)
            : (right.Value, left.Value);

    private static bool HasPlayed(
        EntryId left,
        EntryId right,
        HashSet<(Guid Low, Guid High)> played) =>
        played.Contains(Canonical(left, right));

    private static IEnumerable<EntryId> OrderByeCandidates(
        IReadOnlyList<EntryId> ordered,
        Dictionary<EntryId, SwissParticipantStanding> byEntry,
        IReadOnlyDictionary<EntryId, int> byeCounts) =>
        ordered
            .OrderBy(byeCounts.GetValueOrDefault)
            .ThenByDescending(id => byEntry[id].Position)
            .ThenBy(id => id.Value);

    private static bool TryPair(
        List<EntryId> unpaired,
        List<SwissPairing> acc,
        Dictionary<EntryId, SwissParticipantStanding> byEntry,
        HashSet<(Guid Low, Guid High)> played,
        IReadOnlyDictionary<EntryId, int> homeCounts)
    {
        if (unpaired.Count == 0)
        {
            return true;
        }

        if (unpaired.Count % 2 != 0)
        {
            return false;
        }

        var a = unpaired[0];
        foreach (var b in OrderOpponents(a, unpaired, byEntry, played))
        {
            acc.Add(AssignHomeAway(a, b, homeCounts));
            var next = unpaired.Where(id => !id.Equals(a) && !id.Equals(b)).ToList();
            if (TryPair(next, acc, byEntry, played, homeCounts))
            {
                return true;
            }

            acc.RemoveAt(acc.Count - 1);
        }

        return false;
    }

    private static IEnumerable<EntryId> OrderOpponents(
        EntryId a,
        List<EntryId> unpaired,
        Dictionary<EntryId, SwissParticipantStanding> byEntry,
        HashSet<(Guid Low, Guid High)> played)
    {
        var pointsA = byEntry[a].Points;
        return unpaired
            .Where(b => !b.Equals(a) && !HasPlayed(a, b, played))
            .OrderBy(b => Math.Abs(pointsA - byEntry[b].Points))
            .ThenBy(b => byEntry[b].Position)
            .ThenBy(b => b.Value);
    }

    private static SwissPairing AssignHomeAway(
        EntryId left,
        EntryId right,
        IReadOnlyDictionary<EntryId, int> homeCounts)
    {
        var homesLeft = homeCounts.GetValueOrDefault(left);
        var homesRight = homeCounts.GetValueOrDefault(right);
        return homesLeft != homesRight
            ? homesLeft < homesRight
                ? new SwissPairing(left, right)
                : new SwissPairing(right, left)
            : left.Value.CompareTo(right.Value) <= 0
            ? new SwissPairing(left, right)
            : new SwissPairing(right, left);
    }
}
