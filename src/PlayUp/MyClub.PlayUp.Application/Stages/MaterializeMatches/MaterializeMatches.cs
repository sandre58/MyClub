// -----------------------------------------------------------------------
// <copyright file="MaterializeMatches.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: materialize Fixtures and Matches for Championship / Groups.
/// Cup uses <see cref="MaterializeCupFromOccupiedSlots"/> exclusively (BracketPair → Fixture).
/// </summary>
/// <remarks>
/// Orchestrates Domain primitives only (AddMatchday / AddFixture / Match.Create / AttachMatch).
/// Idempotent when expected directed Home/Away pairs are already attached.
/// Championship / Groups read <see cref="Stage.MatchGenerationFormat"/> (Single or Double Round-Robin + PairMirror).
/// </remarks>
public static class MaterializeMatches
{
    /// <summary>
    /// Materializes matches for the inferred format of the primary stage.
    /// </summary>
    /// <param name="competition">Owning competition (entries).</param>
    /// <param name="stage">Primary stage.</param>
    /// <param name="existingMatches">Matches already loaded for the stage (idempotence).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>Created matches and attached identities.</returns>
    public static MaterializeMatchesResult Execute(
        Competition competition,
        Stage stage,
        IReadOnlyList<Match> existingMatches,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(existingMatches);
        ArgumentNullException.ThrowIfNull(clock);

        if (!stage.CompetitionId.Equals(competition.Id))
        {
            throw new ApplicationFailureException(
                $"Stage '{stage.Id}' does not belong to competition '{competition.Id}'.",
                ApplicationErrorCodes.StageNotInCompetition);
        }

        EnsureMutable(competition, stage);

        var format = InferFormat(stage)
                     ?? throw new ApplicationFailureException(
                         "Stage has no recognised V1 structure for materialization.",
                         ApplicationErrorCodes.MaterializationFailure);

        return format switch
        {
            StructureFormatKind.Championship => MaterializeChampionship(competition, stage, existingMatches, clock),
            StructureFormatKind.Groups => MaterializeGroups(competition, stage, existingMatches, clock),
            StructureFormatKind.Cup => throw new ApplicationFailureException(
                "Cup stages use MaterializeCupFromOccupiedSlots (BracketPair → Fixture) — MaterializeMatches is not applicable.",
                ApplicationErrorCodes.MaterializationFailure),

            // Legacy DBs may still contain unbound Cup fixtures created by the removed skeleton path
            // (no BracketPairKey). Do not auto-delete them here; clean up via a dedicated data task if needed.
            StructureFormatKind.Swiss => throw new ApplicationFailureException(
                "Swiss stages use GenerateNextRound — MaterializeMatches is not applicable.",
                ApplicationErrorCodes.MaterializationFailure),
            _ => throw new ApplicationFailureException(
                $"Materialization does not support format '{format}'.",
                ApplicationErrorCodes.MaterializationFailure)
        };
    }

    private static MaterializeMatchesResult MaterializeChampionship(
        Competition competition,
        Stage stage,
        IReadOnlyList<Match> existingMatches,
        IClock clock)
    {
        var entries = GetActiveEntries(competition);
        if (entries.Count < 2)
        {
            throw new ApplicationFailureException(
                "Championship materialization requires at least two active entries.",
                ApplicationErrorCodes.MaterializationFailure);
        }

        var rounds = BuildRoundRobinRounds(entries, stage.MatchGenerationFormat);
        return MaterializeRoundsOnMatchdays(competition, stage, existingMatches, rounds, clock);
    }

    private static MaterializeMatchesResult MaterializeGroups(
        Competition competition,
        Stage stage,
        IReadOnlyList<Match> existingMatches,
        IClock clock)
    {
        if (stage.Groups.Count == 0)
        {
            throw new ApplicationFailureException(
                "Groups materialization requires groups on the stage.",
                ApplicationErrorCodes.MaterializationFailure);
        }

        var existingByPair = IndexExistingDirectedPairs(stage, existingMatches);

        var maxRounds = 0;
        var perGroupRounds = new List<IReadOnlyList<IReadOnlyList<(EntryId Home, EntryId Away)>>>();
        foreach (var group in stage.Groups)
        {
            if (group.EntryIds.Count < 2)
            {
                throw new ApplicationFailureException(
                    $"Group '{group.Name}' needs at least two assigned entries before materialization (apply the Group draw first).",
                    ApplicationErrorCodes.MaterializationFailure);
            }

            var rounds = BuildRoundRobinRounds([.. group.EntryIds], stage.MatchGenerationFormat);
            perGroupRounds.Add(rounds);
            maxRounds = Math.Max(maxRounds, rounds.Count);
        }

        var expectedTotal = perGroupRounds.Sum(rounds => rounds.Sum(round => round.Count));
        var alreadyPresent = perGroupRounds
            .SelectMany(rounds => rounds.SelectMany(round => round))
            .Count(pair => existingByPair.ContainsKey(DirectedPair(pair.Home, pair.Away)));
        if (alreadyPresent == expectedTotal)
        {
            return new MaterializeMatchesResult(
                [],
                [.. existingMatches.Select(match => match.Id)],
                AlreadyComplete: true);
        }

        EnsureMatchdays(stage, maxRounds, clock);
        var matchdaysByIndex = stage.Matchdays.OrderBy(matchday => matchday.Number).ToList();

        var created = new List<Match>();
        foreach (var rounds in perGroupRounds)
        {
            for (var roundIndex = 0; roundIndex < rounds.Count; roundIndex++)
            {
                var matchday = matchdaysByIndex[roundIndex];
                foreach (var (home, away) in rounds[roundIndex])
                {
                    var key = DirectedPair(home, away);
                    if (existingByPair.ContainsKey(key))
                    {
                        continue;
                    }

                    var fixture = stage.AddFixture(matchday.Id, clock);
                    var match = Match.Create(competition.Id, stage.Id, home, away, clock);
                    stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, clock);
                    created.Add(match);
                    existingByPair[key] = match;
                }
            }
        }

        return new MaterializeMatchesResult(
            created,
            [.. existingByPair.Values.Select(match => match.Id)],
            AlreadyComplete: false);
    }

    private static MaterializeMatchesResult MaterializeRoundsOnMatchdays(
        Competition competition,
        Stage stage,
        IReadOnlyList<Match> existingMatches,
        IReadOnlyList<IReadOnlyList<(EntryId Home, EntryId Away)>> rounds,
        IClock clock)
    {
        var existingByPair = IndexExistingDirectedPairs(stage, existingMatches);

        var expectedPairs = rounds.SelectMany(round => round).ToList();
        var missing = expectedPairs
            .Where(pair => !existingByPair.ContainsKey(DirectedPair(pair.Home, pair.Away)))
            .ToList();

        if (missing.Count == 0)
        {
            return new MaterializeMatchesResult(
                [],
                [.. existingMatches.Select(match => match.Id)],
                AlreadyComplete: true);
        }

        EnsureMatchdays(stage, rounds.Count, clock);
        var matchdaysByIndex = stage.Matchdays.OrderBy(matchday => matchday.Number).ToList();

        var created = new List<Match>();
        for (var roundIndex = 0; roundIndex < rounds.Count; roundIndex++)
        {
            var matchday = matchdaysByIndex[roundIndex];
            foreach (var (home, away) in rounds[roundIndex])
            {
                var key = DirectedPair(home, away);
                if (existingByPair.ContainsKey(key))
                {
                    continue;
                }

                var fixture = stage.AddFixture(matchday.Id, clock);
                var match = Match.Create(competition.Id, stage.Id, home, away, clock);
                stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, clock);
                created.Add(match);
                existingByPair[key] = match;
            }
        }

        var attached = existingByPair.Values.Select(match => match.Id).ToArray();
        return new MaterializeMatchesResult(created, attached, AlreadyComplete: false);
    }

    /// <summary>
    /// Builds single round-robin directed pairs (home/away ordered stably by EntryId).
    /// </summary>
    /// <remarks>Used for combinatorial expectations; packing uses <see cref="BuildRoundRobinRounds"/>.</remarks>
    public static IReadOnlyList<(EntryId Home, EntryId Away)> BuildRoundRobinPairs(IReadOnlyList<EntryId> entries)
    {
        var ordered = entries.OrderBy(id => id.Value).ToList();
        var pairs = new List<(EntryId, EntryId)>();
        for (var i = 0; i < ordered.Count; i++)
        {
            for (var j = i + 1; j < ordered.Count; j++)
            {
                pairs.Add((ordered[i], ordered[j]));
            }
        }

        return pairs;
    }

    /// <summary>
    /// Builds packed matchday rounds for Championship / Groups according to <paramref name="format"/>.
    /// </summary>
    /// <remarks>
    /// DoubleRoundRobin = phase-1 circle packing + PairMirror (swap Home/Away), without H/A streak optimization.
    /// Contract is invariants B1–B6; circle method is the V1 packing implementation only.
    /// </remarks>
    public static IReadOnlyList<IReadOnlyList<(EntryId Home, EntryId Away)>> BuildRoundRobinRounds(
        IReadOnlyList<EntryId> entries,
        MatchGenerationFormat format)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (!Enum.IsDefined(format))
        {
            throw new ApplicationFailureException(
                $"Unknown match generation format '{format}'.",
                ApplicationErrorCodes.MaterializationFailure);
        }

        var phase1 = CircleMethodRounds(entries);
        if (format == MatchGenerationFormat.SingleRoundRobin)
        {
            return phase1;
        }

        var mirrored = phase1
            .Select(round =>
            {
                IReadOnlyList<(EntryId Home, EntryId Away)> mirroredRound =
                    [.. round.Select(pair => (pair.Away, pair.Home))];
                return mirroredRound;
            })
            .ToArray();
        return [.. phase1, .. mirrored];
    }

    /// <summary>
    /// Circle method: packs entries into rounds so each team plays at most once per round.
    /// </summary>
    internal static IReadOnlyList<IReadOnlyList<(EntryId Home, EntryId Away)>> CircleMethodRounds(
        IReadOnlyList<EntryId> entries)
    {
        var ordered = entries.Distinct().OrderBy(id => id.Value).ToList();
        if (ordered.Count < 2)
        {
            return [];
        }

        var working = ordered.ToList();
        var bye = EntryId.New();
        var hasBye = working.Count % 2 != 0;
        if (hasBye)
        {
            working.Add(bye);
        }

        var n = working.Count;
        var rounds = new List<IReadOnlyList<(EntryId, EntryId)>>();
        for (var round = 0; round < n - 1; round++)
        {
            var roundPairs = new List<(EntryId, EntryId)>();
            for (var i = 0; i < n / 2; i++)
            {
                var a = working[i];
                var b = working[n - 1 - i];
                if (hasBye && (a.Equals(bye) || b.Equals(bye)))
                {
                    continue;
                }

                roundPairs.Add(CanonicalOrdered(a, b));
            }

            rounds.Add(roundPairs);

            // Rotate all but first.
            var last = working[^1];
            for (var i = working.Count - 1; i > 1; i--)
            {
                working[i] = working[i - 1];
            }

            working[1] = last;
        }

        return rounds;
    }

    /// <summary>
    /// Overload for callers that still pass combinatorial pairs (extracts entries).
    /// </summary>
    internal static IReadOnlyList<IReadOnlyList<(EntryId Home, EntryId Away)>> CircleMethodRounds(
        IReadOnlyList<(EntryId Home, EntryId Away)> allPairs) =>
        CircleMethodRounds(
            [.. allPairs.SelectMany(pair => new[] { pair.Home, pair.Away }).Distinct()]);

    private static Dictionary<(EntryId Home, EntryId Away), Match> IndexExistingDirectedPairs(
        Stage stage,
        IReadOnlyList<Match> existingMatches) =>
        existingMatches
            .Where(match => stage.HasMatch(match.Id))
            .ToDictionary(
                match => DirectedPair(match.HomeEntryId, match.AwayEntryId),
                match => match);

    private static void EnsureMatchdays(Stage stage, int requiredCount, IClock clock)
    {
        var nextNumber = stage.Matchdays.Count == 0
            ? 1
            : stage.Matchdays.Max(matchday => matchday.Number) + 1;
        while (stage.Matchdays.Count < requiredCount)
        {
            stage.AddMatchday(nextNumber++, clock);
        }
    }

    private static IReadOnlyList<EntryId> GetActiveEntries(Competition competition) =>
    [
        .. competition.Entries
            .Where(entry => entry.Status == EntryStatus.Active)
            .Select(entry => entry.Id)
            .OrderBy(id => id.Value)
    ];

    private static (EntryId Home, EntryId Away) DirectedPair(EntryId home, EntryId away) => (home, away);

    private static (EntryId Home, EntryId Away) CanonicalOrdered(EntryId a, EntryId b) =>
        a.Value.CompareTo(b.Value) <= 0 ? (a, b) : (b, a);

    private static StructureFormatKind? InferFormat(Stage stage) =>
        stage.IsSwiss
            ? StructureFormatKind.Swiss
            : stage.Rounds.Count > 0
                ? StructureFormatKind.Cup
                : stage.Groups.Count > 0
                    ? StructureFormatKind.Groups
                    : stage.Matchdays.Count > 0
                        ? StructureFormatKind.Championship
                        : null;

    private static void EnsureMutable(Competition competition, Stage stage)
    {
        if (competition.Status is CompetitionStatus.Running
            or CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Matches cannot be materialized while competition status is '{competition.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Matches cannot be materialized while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }
    }
}
