// -----------------------------------------------------------------------
// <copyright file="StandingComparer.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Domain.Standings.Rules;
using MyNet.Utilities;

namespace MyClub.Shared.Domain.Standings.Comparers;

/// <summary>
/// Main comparer class for ranking teams in standings tables.
/// Combines multiple comparison criteria to determine the relative order of teams.
/// Implements contextual comparison support and provides enumeration over its constituent comparers.
/// </summary>
/// <param name="comparers">The collection of individual comparers to chain together.</param>
public class StandingComparer(IEnumerable<IStandingComparer> comparers) : StandingRowComparer, IStandingContextualComparer, IEnumerable<IStandingComparer>
{
    private readonly List<IStandingComparer> _comparers = [.. comparers];

    /// <summary>
    /// Gets a dictionary of all available comparer types with their factory methods.
    /// This registry allows dynamic creation of comparers by name.
    /// </summary>
    public static IDictionary<string, Func<IStandingComparer>> AllAvailableComparers => new Dictionary<string, Func<IStandingComparer>>
        {
            { nameof(StandingRowByPointsComparer), static () => new StandingRowByPointsComparer() },
            { nameof(StandingRowByHeadToHeadComparer), static () => new StandingRowByHeadToHeadComparer() },
            { nameof(StandingRowByGoalsDifferenceComparer), static () => new StandingRowByGoalsDifferenceComparer() },
            { nameof(StandingRowByGoalsForComparer), static () => new StandingRowByGoalsForComparer() },
            { nameof(StandingRowByGoalsAgainstComparer), static () => new StandingRowByGoalsAgainstComparer() },
            { nameof(StandingRowByGamesWonComparer), static () => new StandingRowByGamesWonComparer() },
            { nameof(StandingRowByGamesWonAfterShootoutsComparer), static () => new StandingRowByGamesWonAfterShootoutsComparer() },
            { nameof(StandingRowByGamesLostComparer), static () => new StandingRowByGamesLostComparer() },
            { nameof(StandingRowByGamesLostAfterShootoutsComparer), static () => new StandingRowByGamesLostAfterShootoutsComparer() },
            { nameof(StandingRowByGamesWithdrawnComparer), static () => new StandingRowByGamesWithdrawnComparer() },
            { nameof(StandingRowByPenaltyPointsComparer), static () => new StandingRowByPenaltyPointsComparer() }
        };

    /// <summary>
    /// Gets the default comparer configuration commonly used in football leagues.
    /// Ranks teams by: Points → Goal Difference → Head-to-Head → Goals For.
    /// </summary>
    public static StandingComparer Default => new StandingComparerBuilder().ThenByPoints()
                                                                           .ThenBy(StandingColumnType.GoalsDifference)
                                                                           .ThenByHeadToHead()
                                                                           .ThenBy(StandingColumnType.GoalsFor)
                                                                           .Build();

    /// <summary>
    /// Sets the context for contextual comparers within this comparer chain.
    /// Only affects comparers that implement IStandingContextualComparer.
    /// </summary>
    /// <param name="matches">The matches to use for contextual comparisons.</param>
    /// <param name="rules">The standing rules to apply during comparisons.</param>
    public void SetContext(IEnumerable<IMatch> matches, StandingRuleSet rules) => _comparers.OfType<IStandingContextualComparer>().ForEach(comparer => comparer.SetContext(matches, rules));

    /// <summary>
    /// Compares two standing rows using the configured comparison chain.
    /// Returns the result of the first comparer that produces a non-zero result.
    /// </summary>
    /// <param name="x">The first standing row to compare.</param>
    /// <param name="y">The second standing row to compare.</param>
    /// <returns>A value indicating the relative order of the standing rows.</returns>
    protected override int CompareTo(IStandingRow x, IStandingRow y) => _comparers.Select(rule => rule.Compare(x, y)).FirstOrDefault(ruleResult => ruleResult != 0);

    /// <summary>
    /// Returns an enumerator that iterates through the constituent comparers.
    /// </summary>
    /// <returns>An enumerator for the comparer collection.</returns>
    public IEnumerator<IStandingComparer> GetEnumerator() => _comparers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Abstract base class for standing row comparers.
/// Provides null-safe comparison logic and delegates actual comparison to derived classes.
/// </summary>
public abstract class StandingRowComparer : IStandingComparer
{
    /// <summary>
    /// Compares two standing rows with null-safety checks.
    /// Returns 0 if either row is null or if both rows reference the same team.
    /// </summary>
    /// <param name="x">The first standing row to compare.</param>
    /// <param name="y">The second standing row to compare.</param>
    /// <returns>A value indicating the relative order of the standing rows.</returns>
    public int Compare(IStandingRow? x, IStandingRow? y) => x == null || y == null || x == y ? 0 : CompareTo(x, y);

    /// <summary>
    /// Performs the actual comparison between two non-null standing rows.
    /// This method must be implemented by derived classes to define specific comparison logic.
    /// </summary>
    /// <param name="x">The first standing row to compare.</param>
    /// <param name="y">The second standing row to compare.</param>
    /// <returns>A value indicating the relative order of the standing rows.</returns>
    protected abstract int CompareTo(IStandingRow x, IStandingRow y);
}

/// <summary>
/// Comparer that uses a selector function to extract comparable values from standing rows.
/// Supports both ascending and descending sort orders.
/// </summary>
/// <param name="compareExpression">Function that extracts the comparable value from a standing row.</param>
/// <param name="ascending">True for ascending order, false for descending order (default).</param>
public class StandingRowByComparableComparer(Func<IStandingRow, IComparable?> compareExpression, bool ascending = false) : StandingRowComparer
{
    /// <summary>
    /// Compares two standing rows using the configured selector function.
    /// Handles null values appropriately and applies the specified sort direction.
    /// </summary>
    /// <param name="x">The first standing row to compare.</param>
    /// <param name="y">The second standing row to compare.</param>
    /// <returns>A value indicating the relative order of the standing rows.</returns>
    protected override int CompareTo(IStandingRow x, IStandingRow y)
    {
        var compareX = compareExpression(x);
        var compareY = compareExpression(y);

        var ascendingModifier = ascending ? 1 : -1;
        return compareX == null && compareY == null ? 0 : compareX == null ? -ascendingModifier : compareY == null ? ascendingModifier : compareX.CompareTo(compareY) * ascendingModifier;
    }
}

/// <summary>
/// Comparer that sorts standing rows by a specific column value.
/// Supports both string-based column identifiers and typed column enums.
/// </summary>
public class StandingRowByColumnComparer : StandingRowByComparableComparer
{
    public StandingRowByColumnComparer(string column, bool ascending = false)
        : base(x => x.Get<IComparable>(column), ascending) { }

    public StandingRowByColumnComparer(StandingColumnType column, bool ascending = false)
        : base(x => x.Get(column), ascending) { }
}

/// <summary>Comparer that sorts teams by their total points (descending order).</summary>
public class StandingRowByPointsComparer() : StandingRowByComparableComparer(static x => x.Points);

/// <summary>Comparer that sorts teams by their penalty points (descending order).</summary>
public class StandingRowByPenaltyPointsComparer() : StandingRowByComparableComparer(static x => x.PenaltyPoints);

/// <summary>Comparer that sorts teams by goals scored (descending order).</summary>
public class StandingRowByGoalsForComparer() : StandingRowByColumnComparer(StandingColumnType.GoalsFor);

/// <summary>Comparer that sorts teams by goals conceded (ascending order - fewer goals conceded is better).</summary>
public class StandingRowByGoalsAgainstComparer() : StandingRowByColumnComparer(StandingColumnType.GoalsAgainst, true);

/// <summary>Comparer that sorts teams by goal difference (descending order).</summary>
public class StandingRowByGoalsDifferenceComparer() : StandingRowByColumnComparer(StandingColumnType.GoalsDifference);

/// <summary>Comparer that sorts teams by games won in regular time (descending order).</summary>
public class StandingRowByGamesWonComparer() : StandingRowByColumnComparer(StandingColumnType.GamesWon);

/// <summary>Comparer that sorts teams by games won after shootouts (descending order).</summary>
public class StandingRowByGamesWonAfterShootoutsComparer() : StandingRowByColumnComparer(StandingColumnType.GamesWonAfterShootouts);

/// <summary>Comparer that sorts teams by games lost in regular time (ascending order - fewer losses is better).</summary>
public class StandingRowByGamesLostComparer() : StandingRowByColumnComparer(StandingColumnType.GamesLost);

/// <summary>Comparer that sorts teams by games lost after shootouts (ascending order - fewer losses is better).</summary>
public class StandingRowByGamesLostAfterShootoutsComparer() : StandingRowByColumnComparer(StandingColumnType.GamesLostAfterShootouts);

/// <summary>Comparer that sorts teams by games withdrawn (ascending order - fewer withdrawals is better).</summary>
public class StandingRowByGamesWithdrawnComparer() : StandingRowByColumnComparer(StandingColumnType.GamesWithdrawn);

/// <summary>
/// Comparer that sorts teams based on their head-to-head record.
/// Evaluates direct match results between the teams being compared by creating a mini-standing
/// with only those teams and their mutual matches.
/// </summary>
public class StandingRowByHeadToHeadComparer : StandingRowComparer, IStandingContextualComparer
{
    private IEnumerable<IMatch>? _matches;
    private StandingRuleSet? _rules;

    /// <summary>
    /// Sets the context required for head-to-head comparison.
    /// </summary>
    /// <param name="matches">All matches to consider for head-to-head evaluation.</param>
    /// <param name="rules">The standing rules to apply in the head-to-head mini-standing.</param>
    public void SetContext(IEnumerable<IMatch> matches, StandingRuleSet rules)
    {
        _matches = matches;
        _rules = rules;
    }

    /// <summary>
    /// Compares two teams based on their head-to-head record.
    /// Creates a mini-standing with only the matches between these teams and compares their positions.
    /// </summary>
    /// <param name="x">The first standing row to compare.</param>
    /// <param name="y">The second standing row to compare.</param>
    /// <returns>A value indicating which team has the better head-to-head record.</returns>
    protected override int CompareTo(IStandingRow x, IStandingRow y)
    {
        if (_matches is null || _rules is null || x.Team == y.Team)
            return 0;

        var matches = _matches.Where(m => m.HasResult(x.Team) && m.HasResult(y.Team)).ToList();
        var teams = _matches.SelectMany(m => m.GetTeams()).Distinct().ToList();

        if (matches.Count == 0 || teams.Count == 0)
            return 0;

        var rules = new StandingRuleSet(Enum.GetValues<MatchResultType>().ToDictionary(mrt => mrt, _rules.GetPoints), _rules.Columns, new([.. _rules.Comparer.Where(sc => sc is not StandingRowByHeadToHeadComparer)]));
        var standing = new Standing(teams, rules);
        standing.ComputeAll(_matches);

        return standing.GetRank(x.Team).CompareTo(standing.GetRank(y.Team));
    }
}
