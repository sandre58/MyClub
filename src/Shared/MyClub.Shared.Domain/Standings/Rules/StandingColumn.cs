// -----------------------------------------------------------------------
// <copyright file="StandingColumn.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Shared.Domain.Standings.Rules;

/// <summary>
/// Static factory class for creating standard standing columns based on predefined column types.
/// Provides a central registry of all available statistical columns used in standings calculations.
/// </summary>
public static class StandingColumn
{
    /// <summary>
    /// Creates a standing column implementation for the specified column type.
    /// </summary>
    /// <param name="rankingColumn">The type of standing column to create.</param>
    /// <returns>An IStandingColumn implementation that calculates the specified statistic.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an unsupported column type is specified.</exception>
    public static IStandingColumn Create(StandingColumnType rankingColumn)
    => rankingColumn switch
    {
        StandingColumnType.GamesPlayed => new GamesPlayedColumn(),
        StandingColumnType.GamesWon => new GamesWonColumn(),
        StandingColumnType.GamesWonAfterShootouts => new GamesWonAfterShootoutsColumn(),
        StandingColumnType.GamesDrawn => new GamesDrawnColumn(),
        StandingColumnType.GamesLost => new GamesLostColumn(),
        StandingColumnType.GamesLostAfterShootouts => new GamesLostAfterShootoutsColumn(),
        StandingColumnType.GamesWithdrawn => new GamesWithdrawnColumn(),
        StandingColumnType.GoalsFor => new GoalsForColumn(),
        StandingColumnType.GoalsAgainst => new GoalsAgainstColumn(),
        StandingColumnType.GoalsDifference => new GoalsDifferenceColumn(),
        _ => throw new InvalidOperationException("Invalid RankingColumn")
    };
}

/// <summary>
/// Generic implementation of a standing column that calculates statistics for teams.
/// Provides both incremental and batch calculation capabilities with strongly-typed values.
/// </summary>
/// <typeparam name="T">The type of value this column produces.</typeparam>
/// <param name="key">The unique key identifier for this column.</param>
/// <param name="incrementalCalculator">Function that updates a value based on a single match result.</param>
/// <param name="batchCalculator">Function that calculates a value from a complete set of matches.</param>
/// <param name="defaultValue">The default value when no matches have been processed.</param>
public class StandingColumn<T>(string key, Func<T, TeamReference, IMatch, T> incrementalCalculator, Func<TeamReference, IEnumerable<IMatch>, T> batchCalculator, T defaultValue) : IStandingColumn<T>
    where T : notnull
{
    /// <summary>
    /// Gets the unique key identifier for this column.
    /// </summary>
    public string Key { get; } = key;

    /// <summary>
    /// Gets the default value for this column when no matches have been processed.
    /// </summary>
    public T DefaultValue { get; } = defaultValue;

    /// <summary>
    /// Gets the default value as an object for non-generic access.
    /// </summary>
    object IStandingColumn.DefaultValue => DefaultValue;

    /// <summary>
    /// Computes the new value for this column based on a previous value and a single match result.
    /// </summary>
    /// <param name="previousValue">The previous value of this column for the team.</param>
    /// <param name="team">The team reference for which to compute the value.</param>
    /// <param name="match">The match to process for this computation.</param>
    /// <returns>The updated value for this column after processing the match.</returns>
    public T ComputeIncremental(T previousValue, TeamReference team, IMatch match) => incrementalCalculator.Invoke(previousValue, team, match);

    /// <summary>
    /// Computes the new value for this column based on a previous value and a single match result (non-generic version).
    /// </summary>
    /// <param name="previousValue">The previous value of this column for the team.</param>
    /// <param name="team">The team reference for which to compute the value.</param>
    /// <param name="match">The match to process for this computation.</param>
    /// <returns>The updated value for this column after processing the match.</returns>
    object IStandingColumn.ComputeIncremental(object previousValue, TeamReference team, IMatch match) => incrementalCalculator.Invoke((T)previousValue, team, match);

    /// <summary>
    /// Computes the value for this column by processing all provided matches at once.
    /// </summary>
    /// <param name="team">The team reference for which to compute the value.</param>
    /// <param name="matches">All matches to consider for this computation.</param>
    /// <returns>The computed value for this column based on all matches.</returns>
    public T ComputeBatch(TeamReference team, IEnumerable<IMatch> matches) => batchCalculator.Invoke(team, matches);

    /// <summary>
    /// Computes the value for this column by processing all provided matches at once (non-generic version).
    /// </summary>
    /// <param name="team">The team reference for which to compute the value.</param>
    /// <param name="matches">All matches to consider for this computation.</param>
    /// <returns>The computed value for this column based on all matches.</returns>
    object IStandingColumn.ComputeBatch(TeamReference team, IEnumerable<IMatch> matches) => batchCalculator.Invoke(team, matches);
}

/// <summary>
/// Specialized implementation of StandingColumn for integer-based statistics.
/// Automatically handles incremental calculations by adding the single-match result to the previous total.
/// </summary>
/// <param name="key">The unique key identifier for this column.</param>
/// <param name="calculator">Function that calculates the integer value from a set of matches.</param>
/// <param name="defaultValue">The default integer value when no matches have been processed (defaults to 0).</param>
public class StandingIntColumn(string key, Func<TeamReference, IEnumerable<IMatch>, int> calculator, int defaultValue = 0) : StandingColumn<int>(key, (previousValue, team, match) => previousValue + calculator.Invoke(team, [match]), calculator, defaultValue);

/// <summary>Column that counts the total number of games played by a team.</summary>
public class GamesPlayedColumn() : StandingIntColumn(nameof(StandingColumnType.GamesPlayed), (team, matches) => matches.Count(m => m.Participate(team)));

/// <summary>Column that counts the number of games won in regular time.</summary>
public class GamesWonColumn() : StandingIntColumn(nameof(StandingColumnType.GamesWon), (team, matches) => matches.Count(m => m.GetResultOf(team) == MatchResultType.Win));

/// <summary>Column that counts the number of games won after penalty shootouts.</summary>
public class GamesWonAfterShootoutsColumn() : StandingIntColumn(nameof(StandingColumnType.GamesWonAfterShootouts), (team, matches) => matches.Count(m => m.GetResultOf(team) == MatchResultType.WinAfterShootouts));

/// <summary>Column that counts the number of games that ended in a draw.</summary>
public class GamesDrawnColumn() : StandingIntColumn(nameof(StandingColumnType.GamesDrawn), (team, matches) => matches.Count(m => m.GetResultOf(team) == MatchResultType.Draw));

/// <summary>Column that counts the number of games lost in regular time.</summary>
public class GamesLostColumn() : StandingIntColumn(nameof(StandingColumnType.GamesLost), (team, matches) => matches.Count(m => m.GetResultOf(team) == MatchResultType.Loss));

/// <summary>Column that counts the number of games lost after penalty shootouts.</summary>
public class GamesLostAfterShootoutsColumn() : StandingIntColumn(nameof(StandingColumnType.GamesLostAfterShootouts), (team, matches) => matches.Count(m => m.GetResultOf(team) == MatchResultType.LossAfterShootouts));

/// <summary>Column that counts the number of games where the team withdrew or forfeited.</summary>
public class GamesWithdrawnColumn() : StandingIntColumn(nameof(StandingColumnType.GamesWithdrawn), (team, matches) => matches.Count(m => m.GetResultOf(team) == MatchResultType.Withdraw));

/// <summary>Column that sums the total number of goals scored by a team.</summary>
public class GoalsForColumn() : StandingIntColumn(nameof(StandingColumnType.GoalsFor), (team, matches) => matches.Sum(m => m.GoalsFor(team)));

/// <summary>Column that sums the total number of goals conceded by a team.</summary>
public class GoalsAgainstColumn() : StandingIntColumn(nameof(StandingColumnType.GoalsAgainst), (team, matches) => matches.Sum(m => m.GoalsAgainst(team)));

/// <summary>Column that calculates the goal difference (goals for minus goals against).</summary>
public class GoalsDifferenceColumn() : StandingIntColumn(nameof(StandingColumnType.GoalsDifference), (team, matches) => matches.Sum(m => m.GoalsFor(team) - m.GoalsAgainst(team)));
