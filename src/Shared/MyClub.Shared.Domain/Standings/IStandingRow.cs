// -----------------------------------------------------------------------
// <copyright file="IStandingRow.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Shared.Domain.Standings;

/// <summary>
/// Interface representing a single row in a standings table for a team.
/// Contains the team's accumulated statistics, points, and provides methods to access column values
/// and update the row with new match results.
/// </summary>
public interface IStandingRow
{
    /// <summary>
    /// Gets the team reference that this standing row represents.
    /// </summary>
    TeamReference Team { get; }

    /// <summary>
    /// Gets the total points earned by the team according to the standings rules.
    /// This is calculated based on match results and the configured point allocation system.
    /// </summary>
    int Points { get; }

    /// <summary>
    /// Gets the penalty points applied to the team.
    /// These are typically deducted from the team's total for rule violations or administrative penalties.
    /// </summary>
    int PenaltyPoints { get; }

    /// <summary>
    /// Gets the value of a specific column for this team.
    /// Returns null if the column doesn't exist or has no value.
    /// </summary>
    /// <param name="column">The key identifier of the column to retrieve.</param>
    /// <returns>The column value as an object, or null if not found.</returns>
    object? Get(string column);

    /// <summary>
    /// Gets the strongly-typed value of a specific column for this team.
    /// Returns null if the column doesn't exist, has no value, or cannot be cast to the specified type.
    /// </summary>
    /// <typeparam name="T">The expected type of the column value.</typeparam>
    /// <param name="column">The key identifier of the column to retrieve.</param>
    /// <returns>The column value cast to the specified type, or null if not found or not castable.</returns>
    T? Get<T>(string column);

    /// <summary>
    /// Gets the integer value of a standard standing column type for this team.
    /// This is a convenience method for accessing common statistical columns.
    /// </summary>
    /// <param name="column">The type of standing column to retrieve.</param>
    /// <returns>The integer value of the specified column type.</returns>
    int Get(StandingColumnType column);

    /// <summary>
    /// Recomputes all statistics for this team from a complete set of matches.
    /// This method processes all provided matches to recalculate points and column values from scratch.
    /// </summary>
    /// <param name="matches">All matches to consider when recomputing statistics.</param>
    /// <param name="rules">The rule set defining how statistics should be calculated.</param>
    void Compute(IEnumerable<IMatch> matches, StandingRuleSet rules);

    /// <summary>
    /// Updates the team's statistics by applying the results of a single new match.
    /// This method performs an incremental update, adding the match's contribution to existing values.
    /// </summary>
    /// <param name="match">The match to apply to the statistics.</param>
    /// <param name="rules">The rule set defining how the match should be processed.</param>
    void ApplyMatch(IMatch match, StandingRuleSet rules);
}
