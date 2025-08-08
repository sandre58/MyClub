// -----------------------------------------------------------------------
// <copyright file="StandingRuleSet.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Domain.Standings.Comparers;
using MyClub.Shared.Domain.Teams;
using MyNet.Utilities;

namespace MyClub.Shared.Domain.Standings.Rules;

/// <summary>
/// Value object representing a complete set of rules for calculating team standings.
/// Defines how points are awarded for different match results, which statistical columns to include,
/// and how teams should be compared when determining their ranking order.
/// </summary>
/// <param name="pointsByOutcome">Dictionary mapping match result types to point values.</param>
/// <param name="columns">Collection of statistical columns to include in the standings.</param>
/// <param name="comparer">Comparer used to determine the relative ranking of teams.</param>
public sealed class StandingRuleSet(IDictionary<MatchResultType, int> pointsByOutcome, IEnumerable<IStandingColumn> columns, StandingComparer comparer) : ValueObject
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private StandingRuleSet()
        : this(null!, null!, null!) { }

    /// <summary>
    /// Gets the default point allocation system commonly used in football competitions.
    /// Awards 3 points for a win, 1 point for a draw, 0 points for a loss, and -1 point for a withdrawal.
    /// </summary>
    public static Dictionary<MatchResultType, int> DefaultPoints => new()
    {
            { MatchResultType.Win, 3 },
            { MatchResultType.Draw, 1 },
            { MatchResultType.Loss, 0 },
            { MatchResultType.Withdraw, -1 }
    };

    /// <summary>
    /// Gets a default rule set with standard football scoring and all default statistical columns.
    /// This provides a ready-to-use configuration for typical league competitions.
    /// </summary>
    public static readonly StandingRuleSet Default = new(DefaultPoints, DefaultColumns, StandingComparer.Default);

    /// <summary>
    /// Gets the default set of statistical columns included in most standings tables.
    /// Includes all standard game statistics such as games played, won, drawn, lost, goals for/against, etc.
    /// </summary>
    public static IReadOnlyCollection<IStandingColumn> DefaultColumns => [.. Enum.GetValues<StandingColumnType>().Select(StandingColumn.Create)];

    /// <summary>
    /// Gets the comparer used to determine the relative ranking of teams in the standings.
    /// This defines the sort order and tiebreaking rules for the standings table.
    /// </summary>
    public StandingComparer Comparer { get; } = comparer;

    /// <summary>
    /// Gets the point allocation system that maps match result types to point values.
    /// This determines how many points teams receive for wins, draws, losses, etc.
    /// </summary>
    public IReadOnlyDictionary<MatchResultType, int> PointsByOutcome { get; } = new Dictionary<MatchResultType, int>(pointsByOutcome);

    /// <summary>
    /// Gets the collection of statistical columns to include in the standings table.
    /// Each column calculates and displays specific match statistics for teams.
    /// </summary>
    public IReadOnlyCollection<IStandingColumn> Columns { get; } = columns.ToList().AsReadOnly();

    /// <summary>
    /// Gets the number of points awarded for a specific match result type.
    /// Returns 0 if the result type is not defined in the point allocation system.
    /// </summary>
    /// <param name="result">The match result type to get points for.</param>
    /// <returns>The number of points awarded for the specified result type.</returns>
    public int GetPoints(MatchResultType result) => PointsByOutcome.GetValueOrDefault(result);

    /// <summary>
    /// Finds a specific statistical column by its key identifier.
    /// The search is case-insensitive.
    /// </summary>
    /// <param name="column">The key identifier of the column to find.</param>
    /// <returns>The column with the specified key, or null if not found.</returns>
    public IStandingColumn? GetColumn(string column) => Columns.FirstOrDefault(x => string.Equals(x.Key, column, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Computes the total points earned by a team across a collection of matches.
    /// Uses the configured point allocation system to sum up points from all match results.
    /// </summary>
    /// <param name="team">The team reference to compute points for.</param>
    /// <param name="matches">The matches to consider when calculating total points.</param>
    /// <returns>The total number of points earned by the team.</returns>
    public int ComputePoints(TeamReference team, IEnumerable<IMatch> matches) => matches.Sum(m => GetPoints(m.GetResultOf(team)));
}
