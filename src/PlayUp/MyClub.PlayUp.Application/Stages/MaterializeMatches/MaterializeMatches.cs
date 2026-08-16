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
/// Application use case: materialize Fixtures and Matches for Slice 3 (Championship / Groups).
/// Cup Matches come from Pairing <see cref="ApplyDraw"/>; this UC only ensures fixtures / verifies.
/// </summary>
/// <remarks>
/// Orchestrates Domain primitives only (AddMatchday / AddFixture / Match.Create / AttachMatch).
/// Idempotent when expected Home/Away pairs are already attached.
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
            StructureFormatKind.Cup => MaterializeCup(competition, stage, existingMatches, clock),
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

        var pairs = BuildRoundRobinPairs(entries);
        return MaterializePairsOnMatchdays(competition, stage, existingMatches, pairs, clock);
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

        var existingByPair = existingMatches
            .Where(match => stage.HasMatch(match.Id))
            .ToDictionary(
                match => CanonicalPair(match.HomeEntryId, match.AwayEntryId),
                match => match);

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

            var pairs = BuildRoundRobinPairs([.. group.EntryIds]);
            var rounds = CircleMethodRounds(pairs);
            perGroupRounds.Add(rounds);
            maxRounds = Math.Max(maxRounds, rounds.Count);
        }

        var expectedTotal = perGroupRounds.Sum(rounds => rounds.Sum(round => round.Count));
        var alreadyPresent = perGroupRounds
            .SelectMany(rounds => rounds.SelectMany(round => round))
            .Count(pair => existingByPair.ContainsKey(CanonicalPair(pair.Home, pair.Away)));
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
                    var key = CanonicalPair(home, away);
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

    private static MaterializeMatchesResult MaterializeCup(
        Competition competition,
        Stage stage,
        IReadOnlyList<Match> existingMatches,
        IClock clock)
    {
        var entries = GetActiveEntries(competition);
        if (entries.Count < 2 || !IsPowerOfTwo(entries.Count))
        {
            throw new ApplicationFailureException(
                "Cup materialization requires a power-of-two count of active entries (2–64).",
                ApplicationErrorCodes.MaterializationFailure);
        }

        var round = stage.Rounds.FirstOrDefault()
                    ?? throw new ApplicationFailureException(
                        "Cup materialization requires a round on the stage.",
                        ApplicationErrorCodes.MaterializationFailure);

        var expectedFixtures = entries.Count / 2;
        while (round.Fixtures.Count < expectedFixtures)
        {
            stage.AddFixture(round.Id, clock);
        }

        var attached = CollectAttachedMatchIds(stage);
        return attached.Count >= expectedFixtures
            && existingMatches.Count >= expectedFixtures
            && existingMatches.All(match => attached.Contains(match.Id))
            ? new MaterializeMatchesResult([], attached, AlreadyComplete: true)
            : attached.Count == 0
            ?

            // Fixtures ready for Pairing ApplyDraw — Matches are created by Apply, not here.
            new MaterializeMatchesResult([], [], AlreadyComplete: false)
            : new MaterializeMatchesResult([], attached, AlreadyComplete: attached.Count >= expectedFixtures);
    }

    private static MaterializeMatchesResult MaterializePairsOnMatchdays(
        Competition competition,
        Stage stage,
        IReadOnlyList<Match> existingMatches,
        IReadOnlyList<(EntryId Home, EntryId Away)> pairs,
        IClock clock)
    {
        var existingByPair = existingMatches
            .Where(match => stage.HasMatch(match.Id))
            .ToDictionary(
                match => CanonicalPair(match.HomeEntryId, match.AwayEntryId),
                match => match);

        var missing = pairs
            .Where(pair => !existingByPair.ContainsKey(CanonicalPair(pair.Home, pair.Away)))
            .ToList();

        if (missing.Count == 0)
        {
            return new MaterializeMatchesResult(
                [],
                [.. existingMatches.Select(match => match.Id)],
                AlreadyComplete: true);
        }

        var rounds = CircleMethodRounds([.. pairs.Select(pair => (pair.Home, pair.Away))]);
        EnsureMatchdays(stage, rounds.Count, clock);
        var matchdaysByIndex = stage.Matchdays.OrderBy(matchday => matchday.Number).ToList();

        var created = new List<Match>();
        for (var roundIndex = 0; roundIndex < rounds.Count; roundIndex++)
        {
            var matchday = matchdaysByIndex[roundIndex];
            foreach (var (home, away) in rounds[roundIndex])
            {
                var key = CanonicalPair(home, away);
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
    /// Builds single round-robin pairs (home/away ordered stably by EntryId).
    /// </summary>
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
    /// Circle method: packs pairs into rounds so each team plays at most once per round.
    /// </summary>
    internal static IReadOnlyList<IReadOnlyList<(EntryId Home, EntryId Away)>> CircleMethodRounds(
        IReadOnlyList<(EntryId Home, EntryId Away)> allPairs)
    {
        // Prefer classic circle scheduling from the unique entry set for even packing.
        var entries = allPairs
            .SelectMany(pair => new[] { pair.Home, pair.Away })
            .Distinct()
            .OrderBy(id => id.Value)
            .ToList();

        if (entries.Count < 2)
        {
            return [];
        }

        var working = entries.ToList();
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

    private static IReadOnlyList<MatchId> CollectAttachedMatchIds(Stage stage) =>
    [
        .. stage.Matchdays.SelectMany(matchday => matchday.Fixtures)
            .Concat(stage.Rounds.SelectMany(round => round.Fixtures))
            .SelectMany(fixture => fixture.MatchIds)
            .Distinct()
    ];

    private static (EntryId Left, EntryId Right) CanonicalPair(EntryId a, EntryId b) =>
        a.Value.CompareTo(b.Value) <= 0 ? (a, b) : (b, a);

    private static (EntryId Home, EntryId Away) CanonicalOrdered(EntryId a, EntryId b) =>
        a.Value.CompareTo(b.Value) <= 0 ? (a, b) : (b, a);

    private static StructureFormatKind? InferFormat(Stage stage) =>
        stage.Rounds.Count > 0
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
                $"Matches cannot be materialized when competition status is '{competition.Status}'.",
                ApplicationErrorCodes.OrganisationNotMutable);
        }

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Matches cannot be materialized when stage status is '{stage.Status}'.",
                ApplicationErrorCodes.OrganisationNotMutable);
        }
    }

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;
}
