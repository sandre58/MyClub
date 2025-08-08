// -----------------------------------------------------------------------
// <copyright file="IStandingColumn.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Shared.Domain.Standings.Rules;

/// <summary>
/// Generic interface for strongly-typed standing columns that compute statistics for teams.
/// Provides type-safe methods for calculating column values incrementally or in batch mode.
/// </summary>
/// <typeparam name="T">The type of value this column produces (e.g., int for goals, bool for qualifications).</typeparam>
public interface IStandingColumn<T> : IStandingColumn
    where T : notnull
{
    /// <summary>
    /// Gets the default value for this column when no matches have been played.
    /// </summary>
    new T DefaultValue { get; }

    /// <summary>
    /// Computes the new value for this column based on a previous value and a single match result.
    /// This method is used for incremental updates when processing matches one by one.
    /// </summary>
    /// <param name="previousValue">The previous value of this column for the team.</param>
    /// <param name="team">The team reference for which to compute the value.</param>
    /// <param name="match">The match to process for this computation.</param>
    /// <returns>The updated value for this column after processing the match.</returns>
    T ComputeIncremental(T previousValue, TeamReference team, IMatch match);

    /// <summary>
    /// Computes the value for this column by processing all provided matches at once.
    /// This method is used for batch calculations when recomputing standings from scratch.
    /// </summary>
    /// <param name="team">The team reference for which to compute the value.</param>
    /// <param name="matches">All matches to consider for this computation.</param>
    /// <returns>The computed value for this column based on all matches.</returns>
    new T ComputeBatch(TeamReference team, IEnumerable<IMatch> matches);
}

/// <summary>
/// Non-generic base interface for standing columns that compute statistics for teams.
/// Provides object-based methods for calculating column values, allowing for runtime polymorphism.
/// </summary>
public interface IStandingColumn
{
    /// <summary>
    /// Gets the unique key identifying this column.
    /// This key is used to retrieve column values from standing rows.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Gets the default value for this column when no matches have been played.
    /// </summary>
    object DefaultValue { get; }

    /// <summary>
    /// Computes the new value for this column based on a previous value and a single match result.
    /// This method is used for incremental updates when processing matches one by one.
    /// </summary>
    /// <param name="previousValue">The previous value of this column for the team.</param>
    /// <param name="team">The team reference for which to compute the value.</param>
    /// <param name="match">The match to process for this computation.</param>
    /// <returns>The updated value for this column after processing the match.</returns>
    object ComputeIncremental(object previousValue, TeamReference team, IMatch match);

    /// <summary>
    /// Computes the value for this column by processing all provided matches at once.
    /// This method is used for batch calculations when recomputing standings from scratch.
    /// </summary>
    /// <param name="team">The team reference for which to compute the value.</param>
    /// <param name="matches">All matches to consider for this computation.</param>
    /// <returns>The computed value for this column based on all matches.</returns>
    object ComputeBatch(TeamReference team, IEnumerable<IMatch> matches);
}
