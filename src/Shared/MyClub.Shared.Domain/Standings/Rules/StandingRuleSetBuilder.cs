// -----------------------------------------------------------------------
// <copyright file="StandingRuleSetBuilder.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Standings.Comparers;

namespace MyClub.Shared.Domain.Standings.Rules;

/// <summary>
/// Builder class for creating StandingRuleSet instances with a fluent API.
/// Allows configuration of points system, columns, and comparison rules for standings calculations.
/// </summary>
public class StandingRuleSetBuilder
{
    private readonly Dictionary<MatchResultType, int> _points = [];
    private readonly List<IStandingColumn> _columns = [];
    private StandingComparer? _comparer;

    /// <summary>
    /// Configures the points awarded for a specific match result type.
    /// </summary>
    /// <param name="result">The type of match result (Win, Draw, Loss, etc.).</param>
    /// <param name="value">The number of points to award for this result type.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public StandingRuleSetBuilder WithPoints(MatchResultType result, int value)
    {
        _points[result] = value;
        return this;
    }

    /// <summary>
    /// Adds a custom column to the standings table.
    /// </summary>
    /// <param name="column">The column implementation to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public StandingRuleSetBuilder AddColumn(IStandingColumn column)
    {
        _columns.Add(column);
        return this;
    }

    /// <summary>
    /// Adds a standard column to the standings table using a predefined column type.
    /// </summary>
    /// <param name="column">The type of standard column to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public StandingRuleSetBuilder AddColumn(StandingColumnType column) => AddColumn(StandingColumn.Create(column));

    /// <summary>
    /// Adds all default columns to the standings table.
    /// This replaces any previously configured columns with the standard set.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public StandingRuleSetBuilder WithDefaultColumns()
    {
        _columns.Clear();
        _columns.AddRange(StandingRuleSet.DefaultColumns);
        return this;
    }

    /// <summary>
    /// Configures the comparer used to sort teams in the standings.
    /// </summary>
    /// <param name="comparer">The comparer that defines how teams are ranked.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public StandingRuleSetBuilder WithComparer(StandingComparer comparer)
    {
        _comparer = comparer;
        return this;
    }

    /// <summary>
    /// Builds the final StandingRuleSet instance with all configured settings.
    /// </summary>
    /// <returns>A new StandingRuleSet instance with the configured rules.</returns>
    public StandingRuleSet Build() => new(_points, _columns, _comparer ?? StandingComparer.Default);
}
