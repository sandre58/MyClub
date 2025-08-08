// -----------------------------------------------------------------------
// <copyright file="StandingRow.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;
using MyNet.Utilities;

namespace MyClub.Shared.Domain.Standings;

/// <summary>
/// Record representing a single row in a standings table for a specific team.
/// Contains the team's accumulated statistics, points, and provides access to individual column values.
/// Implements IStandingRow to provide a consistent interface for standings calculations.
/// </summary>
/// <param name="Team">The team reference that this standing row represents.</param>
/// <param name="PenaltyPoints">The penalty points applied to this team (defaults to 0).</param>
public record StandingRow(TeamReference Team, int PenaltyPoints = 0) : IStandingRow
{
    private readonly Dictionary<string, object> _columns = [];

    /// <summary>
    /// Gets the total points earned by the team, including any penalty point deductions.
    /// This value is calculated based on match results according to the configured rule set.
    /// </summary>
    public int Points { get; private set; } = PenaltyPoints;

    /// <summary>
    /// Gets the value of a specific column for this team.
    /// Returns null if the column doesn't exist or has no value.
    /// </summary>
    /// <param name="column">The key identifier of the column to retrieve.</param>
    /// <returns>The column value as an object, or null if not found.</returns>
    public object? Get(string column) => _columns.GetOrDefault(column);

    /// <summary>
    /// Gets the strongly-typed value of a specific column for this team.
    /// Returns null if the column doesn't exist, has no value, or cannot be cast to the specified type.
    /// </summary>
    /// <typeparam name="T">The expected type of the column value.</typeparam>
    /// <param name="column">The key identifier of the column to retrieve.</param>
    /// <returns>The column value cast to the specified type, or null if not found or not castable.</returns>
    public T? Get<T>(string column) => (T?)Get(column);

    /// <summary>
    /// Gets the integer value of a standard standing column type for this team.
    /// Returns 0 if the column doesn't exist or has no value.
    /// </summary>
    /// <param name="column">The type of standing column to retrieve.</param>
    /// <returns>The integer value of the specified column type, or 0 if not found.</returns>
    public int Get(StandingColumnType column) => Get<int?>(column.ToString()) ?? 0;

    /// <summary>
    /// Recomputes all statistics for this team from a complete set of matches.
    /// This method processes all provided matches to recalculate points and column values from scratch,
    /// then applies any penalty point deductions.
    /// </summary>
    /// <param name="matches">All matches to consider when recomputing statistics.</param>
    /// <param name="rules">The rule set defining how statistics should be calculated.</param>
    public void Compute(IEnumerable<IMatch> matches, StandingRuleSet rules)
    {
        var matchesList = matches.ToList();
        Points = rules.ComputePoints(Team, matchesList);
        foreach (var column in rules.Columns)
        {
            var value = column.ComputeBatch(Team, matchesList);
            _columns[column.Key] = value;
        }

        Points -= PenaltyPoints;
    }

    /// <summary>
    /// Updates the team's statistics by applying the results of a single new match.
    /// This method performs an incremental update, adding the match's contribution to existing values.
    /// Points are updated based on the match result, and all configured columns are recalculated.
    /// </summary>
    /// <param name="match">The match to apply to the statistics.</param>
    /// <param name="rules">The rule set defining how the match should be processed.</param>
    public void ApplyMatch(IMatch match, StandingRuleSet rules)
    {
        Points += rules.GetPoints(match.GetResultOf(Team));
        foreach (var column in rules.Columns)
        {
            var previousValue = _columns.TryGetValue(column.Key, out var val) ? val : column.DefaultValue;
            _columns[column.Key] = column.ComputeIncremental(previousValue, Team, match);
        }
    }

    /// <summary>
    /// Returns a string representation of this standing row.
    /// Includes the team name, total points, and all statistical column values in a formatted layout.
    /// </summary>
    /// <returns>A formatted string representation of this standing row.</returns>
    public override string ToString()
    {
        var str = new StringBuilder($"{Team} | {Points} PTS | ");

        _ = str.Append(string.Join(" | ", _columns.Select(x => $"{Get(x.Key)} ({x.Key})")));

        return str.ToString();
    }
}
