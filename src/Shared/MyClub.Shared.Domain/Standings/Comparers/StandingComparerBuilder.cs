// -----------------------------------------------------------------------
// <copyright file="StandingComparerBuilder.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using MyClub.Shared.Domain.Enums;

namespace MyClub.Shared.Domain.Standings.Comparers;

public class StandingComparerBuilder
{
    private readonly List<IStandingComparer> _comparers = [];

    public StandingComparerBuilder Then(IStandingComparer comparer)
    {
        _comparers.Add(comparer);
        return this;
    }

    public StandingComparerBuilder ThenBy(string column, bool descending = true) => Then(new StandingRowByColumnComparer(column, descending));

    public StandingComparerBuilder ThenBy(StandingColumnType column, bool descending = true) => Then(new StandingRowByColumnComparer(column, descending));

    public StandingComparerBuilder ThenBy(Func<IStandingRow, IComparable?> selector) => Then(new StandingRowByComparableComparer(selector));

    public StandingComparerBuilder ThenByHeadToHead() => Then(new StandingRowByHeadToHeadComparer());

    public StandingComparerBuilder ThenByPoints() => Then(new StandingRowByPointsComparer());

    public StandingComparer Build() => new(_comparers);
}
