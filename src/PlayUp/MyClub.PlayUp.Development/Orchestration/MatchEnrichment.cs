// -----------------------------------------------------------------------
// <copyright file="MatchEnrichment.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Generators;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Development.Orchestration;

/// <summary>
/// Fills Domain surfaces added after the original seed (rosters, kickoffs, sheets, goals, cards, subs).
/// </summary>
internal static class MatchEnrichment
{
    private const int StarterCount = 11;
    private const int MaxBenchCount = 7;

    private static readonly TimeSpan[] KickoffTimes =
    [
        new(15, 0, 0),
        new(17, 0, 0),
        new(20, 0, 0)
    ];

    public static Regulation WithDiscipline(Regulation regulation)
    {
        ArgumentNullException.ThrowIfNull(regulation);
        return regulation.DisciplinaryRules.AllowedTypes.Count > 0
            ? regulation
            : new Regulation(
                regulation.EntryRules,
                regulation.MatchRules,
                regulation.StandingRules,
                new DisciplinaryRules([DisciplinaryType.Yellow, DisciplinaryType.Red]));
    }

    /// <summary>
    /// Adds standard football extra time (2×15) and TAB (5 kicks) for cup / regulation demos.
    /// Prefer <see cref="SpecializeWithExtraTimeAndPenalties"/> on stages when DefaultsBinding must unbind.
    /// </summary>
    public static Regulation WithExtraTimeAndPenalties(Regulation regulation)
    {
        ArgumentNullException.ThrowIfNull(regulation);
        var match = regulation.MatchRules;
        return new Regulation(
            regulation.EntryRules,
            new MatchRules(
                match.Duration,
                match.AdministrativeResultPolicy,
                new ExtraTimePolicy(durationPerPeriod: 15, numberOfPeriods: 2),
                new PenaltyShootoutPolicy(initialKicksPerTeam: 5)),
            regulation.StandingRules,
            regulation.DisciplinaryRules);
    }

    /// <summary>
    /// Stage specialization: set ET+TAB via <see cref="Stage.ReplaceMatchRules"/> so ExtraTime and
    /// PenaltyShootout unbind while MatchDuration stays bound when unchanged.
    /// </summary>
    public static void SpecializeWithExtraTimeAndPenalties(Stage stage, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);
        var match = stage.Regulation.MatchRules;
        stage.ReplaceMatchRules(
            new MatchRules(
                match.Duration,
                match.AdministrativeResultPolicy,
                new ExtraTimePolicy(durationPerPeriod: 15, numberOfPeriods: 2),
                new PenaltyShootoutPolicy(initialKicksPerTeam: 5)),
            clock);
    }

    public static void ApplyRandomCompetitionSchedule(
        ScenarioContext context,
        Competition competition,
        DateTimeOffset? datasetStart,
        DateTimeOffset? datasetEnd)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(competition);

        if (datasetStart is not null || datasetEnd is not null)
        {
            // Same draw as the generic path so later entropy stays aligned.
            _ = context.Entropy.Next(10);
            competition.SetSchedule(datasetStart, datasetEnd, context.Clock);
            return;
        }

        var start = new DateTimeOffset(
            2025,
            context.Entropy.NextInclusive(7, 9),
            context.Entropy.NextInclusive(1, 28),
            0,
            0,
            0,
            TimeSpan.Zero);
        var end = start.AddMonths(context.Entropy.NextInclusive(6, 10))
            .AddDays(context.Entropy.NextInclusive(0, 20));
        if (end < start)
        {
            end = start.AddMonths(8);
        }

        // 0–1 none, 2 start only, 3–9 both — generic scenarios only (no dataset dates).
        var mode = context.Entropy.Next(10);
        switch (mode)
        {
            case <= 1:
                return;
            case 2:
                competition.SetSchedule(start, null, context.Clock);
                return;
            default:
                competition.SetSchedule(start, end, context.Clock);
                break;
        }
    }

    public static void SeedRosters(ScenarioContext context, Competition competition)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(competition);

        var entries = competition.Entries.OrderBy(entry => entry.Id.Value).ToList();
        for (var teamIndex = 0; teamIndex < entries.Count; teamIndex++)
        {
            var entry = entries[teamIndex];
            if (entry.DeclaredMembers.Count > 0)
            {
                continue;
            }

            var squad = SquadGenerator.ForTeam(teamIndex, entry.DisplayName);
            for (var memberIndex = 0; memberIndex < squad.Count; memberIndex++)
            {
                var member = squad[memberIndex];
                competition.AddDeclaredMember(
                    entry.Id,
                    member.DisplayName,
                    member.Role,
                    context.Ids.Member($"e-{entry.Id.Value:N}-m-{memberIndex}"),
                    context.Clock);
            }
        }
    }

    public static void ApplyKickoffs(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        IReadOnlyList<Match> matches)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(matches);

        var pending = matches
            .Where(match => match.StageId.Equals(stage.Id) && !stage.TryGetMatchPlacement(match.Id, out _))
            .OrderBy(match => match.Id.Value)
            .ToList();
        if (pending.Count == 0)
        {
            return;
        }

        var horizonStart = competition.ScheduledStart
                           ?? new DateTimeOffset(2025, 8, 16, 0, 0, 0, TimeSpan.Zero);
        var horizonEnd = competition.ScheduledEnd ?? horizonStart.AddMonths(9);
        if (horizonEnd <= horizonStart)
        {
            horizonEnd = horizonStart.AddMonths(9);
        }

        var spanTicks = Math.Max(TimeSpan.FromDays(1).Ticks, (horizonEnd - horizonStart).Ticks);
        var resourceId = context.Ids.Resource("pitch-1");
        var placements = new List<MatchPlacement>();
        var targets = new List<MatchId>();

        for (var i = 0; i < pending.Count; i++)
        {
            if (!context.Entropy.Chance(70))
            {
                continue;
            }

            var match = pending[i];
            var day = horizonStart.AddTicks(spanTicks * i / Math.Max(1, pending.Count));
            var saturday = day.Date.AddDays(((int)DayOfWeek.Saturday - (int)day.DayOfWeek + 7) % 7);
            var kickoffDate = DateOnly.FromDateTime(saturday);
            var time = KickoffTimes[context.Entropy.Next(KickoffTimes.Length)];
            var kickoff = new DateTimeOffset(kickoffDate.ToDateTime(TimeOnly.FromTimeSpan(time)), TimeSpan.Zero);
            placements.Add(new MatchPlacement(match.Id, kickoff, resourceId));
            targets.Add(match.Id);
        }

        if (targets.Count > 0)
        {
            stage.ApplyMatchPlacements(placements, targets);
        }
    }

    public static void PlayMatches(
        ScenarioContext context,
        Competition competition,
        IReadOnlyList<Match> matches,
        int count,
        bool decisive)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(matches);

        var ordered = matches.OrderBy(match => match.Id.Value).ToList();
        var toPlay = Math.Min(count, ordered.Count);
        for (var i = 0; i < toPlay; i++)
        {
            context.Clock.Advance(TimeSpan.FromHours(2) + TimeSpan.FromMinutes(i));
            var (home, away) = ScoreGenerator.Create(context.Entropy);
            if (decisive && home == away)
            {
                home++;
            }

            var match = ordered[i];
            EnsureSheet(context, competition, match, required: true);
            match.Finish(ResultGenerator.Played(home, away), context.Clock);
            RecordFacts(context, match, home, away);
        }

        for (var i = toPlay; i < ordered.Count; i++)
        {
            if (context.Entropy.Chance(55))
            {
                EnsureSheet(context, competition, ordered[i], required: false);
            }
        }
    }

    private static void EnsureSheet(
        ScenarioContext context,
        Competition competition,
        Match match,
        bool required)
    {
        if (match.DeclaredParticipations.Count > 0)
        {
            return;
        }

        DeclareSide(context, competition, match, match.HomeEntryId, Side.Home, required);
        DeclareSide(context, competition, match, match.AwayEntryId, Side.Away, required);
    }

    private static void DeclareSide(
        ScenarioContext context,
        Competition competition,
        Match match,
        EntryId entryId,
        Side side,
        bool required)
    {
        var entry = competition.GetEntry(entryId);
        var players = entry.DeclaredMembers
            .Where(member => member.Role == DeclaredMemberRole.Player)
            .ToList();
        if (players.Count < StarterCount)
        {
            if (required)
            {
                throw new InvalidOperationException(
                    $"Entry '{entryId}' needs at least {StarterCount} declared players to seed a match sheet.");
            }

            return;
        }

        context.Entropy.Shuffle(players);
        var take = Math.Min(players.Count, StarterCount + MaxBenchCount);
        for (var i = 0; i < take; i++)
        {
            var player = players[i];
            var status = i < StarterCount ? CompositionStatus.Starter : CompositionStatus.Bench;
            match.AddDeclaredParticipation(
                player.Id,
                side,
                status,
                context.Clock,
                SquadGenerator.ResolveJersey(entry, player));
        }
    }

    private static void RecordFacts(ScenarioContext context, Match match, int homeGoals, int awayGoals)
    {
        RecordGoals(context, match, Side.Home, homeGoals);
        RecordGoals(context, match, Side.Away, awayGoals);
        RecordCards(context, match);
        RecordSubstitutions(context, match, Side.Home);
        RecordSubstitutions(context, match, Side.Away);
    }

    private static void RecordGoals(ScenarioContext context, Match match, Side creditedSide, int goals)
    {
        var credited = OnSheet(match, creditedSide);
        var opponents = OnSheet(match, creditedSide == Side.Home ? Side.Away : Side.Home);
        if (credited.Count == 0)
        {
            return;
        }

        for (var i = 0; i < goals; i++)
        {
            var ownGoal = opponents.Count > 0 && context.Entropy.Chance(6);
            if (ownGoal)
            {
                var scorer = opponents[context.Entropy.Next(opponents.Count)];
                match.RecordGoal(scorer, creditedSide, context.Clock);
                continue;
            }

            var scorerMember = credited[context.Entropy.Next(credited.Count)];
            MemberId? assister = null;
            if (credited.Count > 1 && context.Entropy.Chance(55))
            {
                var others = credited.Where(id => !id.Equals(scorerMember)).ToList();
                assister = others[context.Entropy.Next(others.Count)];
            }

            match.RecordGoal(scorerMember, creditedSide, context.Clock, assister);
        }
    }

    private static void RecordCards(ScenarioContext context, Match match)
    {
        var pool = match.DeclaredParticipations.Select(participation => participation.Id).ToList();
        if (pool.Count == 0)
        {
            return;
        }

        var yellows = context.Entropy.NextInclusive(0, 4);
        for (var i = 0; i < yellows; i++)
        {
            var member = pool[context.Entropy.Next(pool.Count)];
            match.RecordDisciplinaryEvent(member, DisciplinaryType.Yellow, context.Clock);
        }

        if (!context.Entropy.Chance(12)) return;

        var member1 = pool[context.Entropy.Next(pool.Count)];
        match.RecordDisciplinaryEvent(member1, DisciplinaryType.Red, context.Clock);
    }

    private static void RecordSubstitutions(ScenarioContext context, Match match, Side side)
    {
        var starters = match.DeclaredParticipations
            .Where(participation =>
                participation.Side == side && participation.CompositionStatus == CompositionStatus.Starter)
            .Select(participation => participation.Id)
            .ToList();
        var bench = match.DeclaredParticipations
            .Where(participation =>
                participation.Side == side && participation.CompositionStatus == CompositionStatus.Bench)
            .Select(participation => participation.Id)
            .ToList();
        if (starters.Count == 0 || bench.Count == 0)
        {
            return;
        }

        context.Entropy.Shuffle(starters);
        context.Entropy.Shuffle(bench);
        var count = Math.Min(context.Entropy.NextInclusive(0, 3), Math.Min(starters.Count, bench.Count));
        for (var i = 0; i < count; i++)
        {
            match.RecordSubstitution(starters[i], bench[i], side, context.Clock);
        }
    }

    private static IReadOnlyList<MemberId> OnSheet(Match match, Side side) =>
    [
        .. match.DeclaredParticipations
            .Where(participation => participation.Side == side)
            .Select(participation => participation.Id)
    ];
}
