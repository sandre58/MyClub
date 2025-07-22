// -----------------------------------------------------------------------
// <copyright file="StandingRuleSetBuilder.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Standings.Comparers;

namespace MyClub.Shared.Domain.Standings.Rules;

public class StandingRuleSetBuilder
{
    private readonly Dictionary<MatchResultType, int> _points = [];
    private readonly List<IStandingColumn> _columns = [];
    private StandingComparer? _comparer;

    public StandingRuleSetBuilder WithPoints(MatchResultType result, int value)
    {
        _points[result] = value;
        return this;
    }

    public StandingRuleSetBuilder AddColumn(IStandingColumn column)
    {
        _columns.Add(column);
        return this;
    }

    public StandingRuleSetBuilder AddColumn(StandingColumnType column) => AddColumn(StandingColumn.Create(column));

    public StandingRuleSetBuilder WithDefaultColumns()
    {
        _columns.Clear();
        _columns.AddRange(StandingRuleSet.DefaultColumns);
        return this;
    }

    public StandingRuleSetBuilder WithComparer(StandingComparer comparer)
    {
        _comparer = comparer;
        return this;
    }

    public StandingRuleSet Build() => new(_points, _columns, _comparer ?? StandingComparer.Default);
}
