// -----------------------------------------------------------------------
// <copyright file="StandingCalculator.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Domain.Standings;

/// <summary>
/// Pure standing calculation from participants, finished match snapshots, rules, match filter, and optional penalties.
/// Head-to-head ranking uses the same <see cref="MatchFilter"/> as global statistics.
/// Penalties adjust global points only (not the head-to-head mini-table).
/// </summary>
public static class StandingCalculator
{
    /// <summary>
    /// Calculates an ordered standing view.
    /// </summary>
    /// <param name="participants">Entries included in the ranking universe (non-empty, unique).</param>
    /// <param name="matches">Finished match snapshots (may include non-participants; ignored for them).</param>
    /// <param name="rules">Standing rules (points + ordered criteria).</param>
    /// <param name="filter">Which side of each match counts for each entry.</param>
    /// <param name="penalties">
    /// Optional global point deductions. Applied after match points and before ranking.
    /// Host/Application must supply stage penalties here; they must not subtract points themselves.
    /// </param>
    /// <returns>A calculated standing view.</returns>
    public static Standing Calculate(
        IReadOnlyList<EntryId> participants,
        IReadOnlyList<StandingMatch> matches,
        StandingRules rules,
        MatchFilter filter = MatchFilter.All,
        IReadOnlyList<StandingPenalty>? penalties = null)
    {
        ArgumentNullException.ThrowIfNull(participants);
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(rules);

        if (participants.Count == 0)
        {
            throw new DomainException(
                "Standing requires at least one participant.",
                StandingErrorCodes.ParticipantsInvalid);
        }

        if (participants.Distinct().Count() != participants.Count)
        {
            throw new DomainException(
                "Standing participants must be unique.",
                StandingErrorCodes.ParticipantsInvalid);
        }

        if (!Enum.IsDefined(filter))
        {
            throw new DomainException(
                "Match filter is unknown.",
                StandingErrorCodes.MatchFilterInvalid);
        }

        var participantSet = participants.ToHashSet();
        var stats = participants.ToDictionary(id => id, _ => new MutableStats());

        foreach (var match in matches)
        {
            ApplyMatch(stats, participantSet, match, filter, rules.Points);
        }

        ApplyPenalties(stats, participantSet, penalties);

        var rows = participants
            .Select(id =>
            {
                var s = stats[id];
                return new RankableRow(id, s.Played, s.Wins, s.Draws, s.Losses, s.GoalsFor, s.GoalsAgainst, s.Points);
            })
            .ToList();

        var ordered = Rank(rows, rules.RankingCriteria, matches, rules.Points, filter);
        var result = new StandingRow[ordered.Count];
        for (var i = 0; i < ordered.Count; i++)
        {
            var row = ordered[i];
            result[i] = new StandingRow(
                row.EntryId,
                position: i + 1,
                row.Played,
                row.Wins,
                row.Draws,
                row.Losses,
                row.GoalsFor,
                row.GoalsAgainst,
                row.Points);
        }

        return new Standing(result);
    }

    private static void ApplyPenalties(
        Dictionary<EntryId, MutableStats> stats,
        HashSet<EntryId> participants,
        IReadOnlyList<StandingPenalty>? penalties)
    {
        if (penalties is null || penalties.Count == 0)
        {
            return;
        }

        foreach (var penalty in penalties)
        {
            if (!participants.Contains(penalty.EntryId))
            {
                continue;
            }

            stats[penalty.EntryId].Points -= penalty.PointsDeducted;
        }
    }

    private static void ApplyMatch(
        Dictionary<EntryId, MutableStats> stats,
        HashSet<EntryId> participants,
        StandingMatch match,
        MatchFilter filter,
        PointsPolicy points)
    {
        var homeIn = participants.Contains(match.HomeEntryId);
        var awayIn = participants.Contains(match.AwayEntryId);
        switch (homeIn)
        {
            case false when !awayIn:
                return;
            case true when filter is MatchFilter.All or MatchFilter.Home:
                ApplySide(stats[match.HomeEntryId], match.HomeGoals, match.AwayGoals, points);
                break;
            default:
                break;
        }

        if (awayIn && filter is MatchFilter.All or MatchFilter.Away)
        {
            ApplySide(stats[match.AwayEntryId], match.AwayGoals, match.HomeGoals, points);
        }
    }

    private static void ApplySide(MutableStats stats, int goalsFor, int goalsAgainst, PointsPolicy points)
    {
        stats.Played++;
        stats.GoalsFor += goalsFor;
        stats.GoalsAgainst += goalsAgainst;

        if (goalsFor > goalsAgainst)
        {
            stats.Wins++;
            stats.Points += points.WinPoints;
        }
        else if (goalsFor == goalsAgainst)
        {
            stats.Draws++;
            stats.Points += points.DrawPoints;
        }
        else
        {
            stats.Losses++;
            stats.Points += points.LossPoints;
        }
    }

    private static List<RankableRow> Rank(
        List<RankableRow> rows,
        IReadOnlyList<RankingCriterion> criteria,
        IReadOnlyList<StandingMatch> matches,
        PointsPolicy points,
        MatchFilter filter)
    {
        var groups = new List<List<RankableRow>> { rows };
        foreach (var criterion in criteria)
        {
            var nextGroups = new List<List<RankableRow>>();
            foreach (var group in groups)
            {
                if (group.Count <= 1)
                {
                    nextGroups.Add(group);
                    continue;
                }

                nextGroups.AddRange(
                    criterion == RankingCriterion.HeadToHead
                        ? PartitionByHeadToHead(group, matches, points, filter)
                        : PartitionByGlobalCriterion(group, criterion));
            }

            groups = nextGroups;
        }

        return [.. groups.SelectMany(g => g)];
    }

    private static List<List<RankableRow>> PartitionByGlobalCriterion(
        List<RankableRow> group,
        RankingCriterion criterion)
    {
        var ordered = group
            .OrderByDescending(r => GlobalKey(r, criterion))
            .ThenBy(r => r.EntryId.Value)
            .ToList();

        return PartitionEqual(ordered, r => GlobalKey(r, criterion));
    }

    private static int GlobalKey(RankableRow row, RankingCriterion criterion) =>
        criterion switch
        {
            RankingCriterion.Points => row.Points,
            RankingCriterion.GoalDifference => row.GoalDifference,
            RankingCriterion.GoalsFor => row.GoalsFor,
            RankingCriterion.GoalsAgainst => -row.GoalsAgainst,
            RankingCriterion.Wins => row.Wins,

            // Head-to-head is partitioned separately; it must not reach GlobalKey.
            RankingCriterion.HeadToHead => throw new InvalidOperationException(
                "Head-to-head ranking must use PartitionByHeadToHead, not GlobalKey."),
            _ => throw new ArgumentOutOfRangeException(nameof(criterion), criterion, null)
        };

    private static List<List<RankableRow>> PartitionByHeadToHead(
        List<RankableRow> group,
        IReadOnlyList<StandingMatch> matches,
        PointsPolicy points,
        MatchFilter filter)
    {
        var tiedIds = group.Select(r => r.EntryId).ToHashSet();
        var miniStats = group.ToDictionary(r => r.EntryId, _ => new MutableStats());

        foreach (var match in matches)
        {
            if (!tiedIds.Contains(match.HomeEntryId) || !tiedIds.Contains(match.AwayEntryId))
            {
                continue;
            }

            // Same MatchFilter as global stats: H2H is an internal ranking step on filtered matches.
            ApplyMatch(miniStats, tiedIds, match, filter, points);
        }

        var ordered = group
            .OrderByDescending(r => miniStats[r.EntryId].Points)
            .ThenByDescending(r => miniStats[r.EntryId].GoalsFor - miniStats[r.EntryId].GoalsAgainst)
            .ThenByDescending(r => miniStats[r.EntryId].GoalsFor)
            .ThenBy(r => r.EntryId.Value)
            .ToList();

        return PartitionEqual(
            ordered,
            r =>
            {
                var s = miniStats[r.EntryId];
                return (s.Points, s.GoalsFor - s.GoalsAgainst, s.GoalsFor);
            });
    }

    private static List<List<RankableRow>> PartitionEqual<TKey>(
        List<RankableRow> ordered,
        Func<RankableRow, TKey> keySelector)
        where TKey : notnull
    {
        var result = new List<List<RankableRow>>();
        List<RankableRow>? current = null;
        TKey? currentKey = default;
        var hasKey = false;

        foreach (var row in ordered)
        {
            var key = keySelector(row);
            if (!hasKey || !EqualityComparer<TKey>.Default.Equals(currentKey, key))
            {
                current = [];
                result.Add(current);
                currentKey = key;
                hasKey = true;
            }

            current!.Add(row);
        }

        return result;
    }

    private sealed class MutableStats
    {
        public int Played { get; set; }

        public int Wins { get; set; }

        public int Draws { get; set; }

        public int Losses { get; set; }

        public int GoalsFor { get; set; }

        public int GoalsAgainst { get; set; }

        public int Points { get; set; }
    }

    private sealed record RankableRow(
        EntryId EntryId,
        int Played,
        int Wins,
        int Draws,
        int Losses,
        int GoalsFor,
        int GoalsAgainst,
        int Points)
    {
        public int GoalDifference => GoalsFor - GoalsAgainst;
    }
}
