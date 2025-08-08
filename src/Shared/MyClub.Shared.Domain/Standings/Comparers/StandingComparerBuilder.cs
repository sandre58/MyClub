// -----------------------------------------------------------------------
// <copyright file="StandingComparerBuilder.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using MyClub.Shared.Domain.Enums;

namespace MyClub.Shared.Domain.Standings.Comparers;

/// <summary>
/// Builder class for creating StandingComparer instances with a fluent API.
/// Allows chaining multiple comparison criteria to create complex sorting rules for standings tables.
/// The comparers are evaluated in the order they are added, with the first returning a non-zero result determining the final order.
/// </summary>
public class StandingComparerBuilder
{
    private readonly List<IStandingComparer> _comparers = [];

    /// <summary>
    /// Adds a custom comparer to the comparison chain.
    /// </summary>
    /// <param name="comparer">The comparer to add to the chain.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public StandingComparerBuilder Then(IStandingComparer comparer)
    {
        _comparers.Add(comparer);
        return this;
    }

    /// <summary>
    /// Adds a column-based comparer using a string column identifier.
    /// </summary>
    /// <param name="column">The column identifier to compare by.</param>
    /// <param name="descending">True for descending order (default), false for ascending order.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public StandingComparerBuilder ThenBy(string column, bool descending = true) => Then(new StandingRowByColumnComparer(column, descending));

    /// <summary>
    /// Adds a column-based comparer using a predefined column type.
    /// </summary>
    /// <param name="column">The column type to compare by.</param>
    /// <param name="descending">True for descending order (default), false for ascending order.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public StandingComparerBuilder ThenBy(StandingColumnType column, bool descending = true) => Then(new StandingRowByColumnComparer(column, descending));

    /// <summary>
    /// Adds a custom comparer using a selector function that extracts a comparable value from each standing row.
    /// </summary>
    /// <param name="selector">Function that extracts the value to compare from each standing row.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public StandingComparerBuilder ThenBy(Func<IStandingRow, IComparable?> selector) => Then(new StandingRowByComparableComparer(selector));

    /// <summary>
    /// Adds a head-to-head comparer that considers direct match results between teams.
    /// This comparer evaluates the results of matches played directly between the teams being compared.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public StandingComparerBuilder ThenByHeadToHead() => Then(new StandingRowByHeadToHeadComparer());

    /// <summary>
    /// Adds a points-based comparer that sorts teams by their total points.
    /// Teams with more points are ranked higher.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public StandingComparerBuilder ThenByPoints() => Then(new StandingRowByPointsComparer());

    /// <summary>
    /// Builds the final StandingComparer instance with all configured comparison criteria.
    /// </summary>
    /// <returns>A new StandingComparer that applies all the configured comparison rules in order.</returns>
    public StandingComparer Build() => new(_comparers);
}
