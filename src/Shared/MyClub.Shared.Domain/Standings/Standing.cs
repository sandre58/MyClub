// -----------------------------------------------------------------------
// <copyright file="Standing.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;
using MyNet.Utilities;

namespace MyClub.Shared.Domain.Standings;

/// <summary>
/// Represents a complete standings table for a collection of teams in a competition.
/// Manages ranking, statistics, and provides methods for updating standings based on match results.
/// Implements IReadOnlyCollection to allow enumeration over the standing rows.
/// </summary>
/// <param name="teams">The teams to include in the standings table.</param>
/// <param name="rules">The rule set defining how standings are calculated. Uses default rules if not specified.</param>
/// <param name="penaltyPoints">Optional penalty points to be applied to specific teams.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "It's a special collection.")]
public class Standing(IEnumerable<TeamReference> teams, StandingRuleSet? rules = null, IReadOnlyDictionary<TeamReference, int>? penaltyPoints = null) : IReadOnlyCollection<StandingRow>
{
    private readonly Dictionary<TeamReference, StandingRow> _rows = teams.ToDictionary(x => x, x => new StandingRow(x, penaltyPoints?.GetValueOrDefault(x) ?? 0));
    private readonly List<IMatch> _appliedMatches = [];
    private Dictionary<TeamReference, int>? _rankCache;

    /// <summary>
    /// Gets the rule set used for calculating standings.
    /// Contains the point system, columns, and comparison logic.
    /// </summary>
    public StandingRuleSet Rules { get; } = rules ?? StandingRuleSet.Default;

    /// <summary>
    /// Gets the standing rows sorted according to the configured ranking rules.
    /// The first row represents the highest-ranked team, and so on.
    /// </summary>
    public IEnumerable<StandingRow> SortedRows => _rows.Values.OrderBy(static r => r, Rules.Comparer);

    /// <summary>
    /// Gets the number of teams in the standings table.
    /// </summary>
    public int Count => _rows.Count;

    /// <summary>
    /// Gets the current rank (position) of the specified team in the standings.
    /// Ranks start from 1 for the top-positioned team.
    /// </summary>
    /// <param name="team">The team reference to get the rank for.</param>
    /// <returns>The team's current rank, or -1 if the team is not in the standings.</returns>
    public int GetRank(TeamReference team)
    {
        if (_rankCache is null)
            RebuildRankCache();

        return _rankCache?[team] ?? -1;
    }

    /// <summary>
    /// Determines whether the specified team is included in this standings table.
    /// </summary>
    /// <param name="team">The team reference to check for.</param>
    /// <returns>true if the team is in the standings; otherwise, false.</returns>
    public bool Contains(TeamReference team) => _rows.ContainsKey(team);

    /// <summary>
    /// Gets the standing row for the specified team.
    /// </summary>
    /// <param name="team">The team reference to get the row for.</param>
    /// <returns>The StandingRow containing the team's statistics and points.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the team is not in the standings.</exception>
    public StandingRow GetRow(TeamReference team) => _rows[team];

    /// <summary>
    /// Gets the strongly-typed value of a specific column for the specified team.
    /// </summary>
    /// <typeparam name="T">The expected type of the column value.</typeparam>
    /// <param name="team">The team reference to get the column value for.</param>
    /// <param name="column">The key identifier of the column to retrieve.</param>
    /// <returns>The column value cast to the specified type, or null if not found or not castable.</returns>
    public T? GetColumn<T>(TeamReference team, string column) => GetRow(team).Get<T>(column);

    /// <summary>
    /// Gets the integer value of a standard standing column type for the specified team.
    /// </summary>
    /// <param name="team">The team reference to get the column value for.</param>
    /// <param name="column">The type of standing column to retrieve.</param>
    /// <returns>The integer value of the specified column type.</returns>
    public int GetColumn(TeamReference team, StandingColumnType column) => GetRow(team).Get(column);

    /// <summary>
    /// Applies the result of a single match to update the standings incrementally.
    /// Only processes matches that have results. Updates team statistics and invalidates the rank cache.
    /// </summary>
    /// <param name="match">The match to apply to the standings.</param>
    public void ApplyMatch(IMatch match)
    {
        if (!match.HasResult()) return;

        match.GetTeams().ForEach(x => _rows.GetValueOrDefault(x)?.ApplyMatch(match, Rules));

        _appliedMatches.Add(match);
        Rules.Comparer.SetContext(_appliedMatches, Rules);
        SetComparerContext();

        InvalidateRankCache();
    }

    /// <summary>
    /// Applies the results of multiple matches to update the standings incrementally.
    /// Processes each match in order, updating statistics as each match is applied.
    /// </summary>
    /// <param name="matches">The matches to apply to the standings.</param>
    public void ApplyMatches(IEnumerable<IMatch> matches) => matches.ForEach(ApplyMatch);

    /// <summary>
    /// Recomputes the entire standings table from scratch using the provided matches.
    /// This method replaces all current statistics with fresh calculations based on the match set.
    /// </summary>
    /// <param name="matches">All matches to consider when recomputing the standings.</param>
    public void ComputeAll(IEnumerable<IMatch> matches)
    {
        var newAppliedMatches = matches.Where(m => m.HasResult()).ToList();
        var matchesByTeam = newAppliedMatches.SelectMany(m => m.GetTeams().Select(team => (team, m)))
                                             .GroupBy(x => x.team, x => x.m)
                                             .ToDictionary(g => g.Key, g => g.AsEnumerable());

        _rows.Values.ForEach(x => x.Compute(matchesByTeam.GetValueOrDefault(x.Team, []), Rules));

        _appliedMatches.Set(newAppliedMatches);
        SetComparerContext();

        InvalidateRankCache();
    }

    private void InvalidateRankCache() => _rankCache = null;

    private void RebuildRankCache() => _rankCache = SortedRows
            .Select(static (row, index) => new { row.Team, Rank = index + 1 })
            .ToDictionary(static x => x.Team, static x => x.Rank);

    private void SetComparerContext() => Rules.Comparer.SetContext(_appliedMatches, Rules);

    /// <summary>
    /// Returns an enumerator that iterates through the standing rows in sorted order.
    /// </summary>
    /// <returns>An enumerator for the sorted standing rows.</returns>
    public IEnumerator<StandingRow> GetEnumerator() => SortedRows.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Returns a string representation of the complete standings table.
    /// Shows each team's rank, name, points, and statistics in a formatted layout.
    /// </summary>
    /// <returns>A formatted string representation of the standings.</returns>
    public override string ToString()
    {
        var str = new StringBuilder();

        SortedRows.ForEach((x, index) => str.AppendLine(CultureInfo.CurrentCulture, $"{index + 1} : {x.ToString()}"));

        return str.ToString();
    }
}
