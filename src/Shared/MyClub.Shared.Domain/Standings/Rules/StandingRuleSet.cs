// -----------------------------------------------------------------------
// <copyright file="StandingRuleSet.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matchs;
using MyClub.Shared.Domain.Standings.Comparers;
using MyClub.Shared.Domain.Teams;
using MyNet.Utilities;

namespace MyClub.Shared.Domain.Standings.Rules;

public class StandingRuleSet(IDictionary<MatchResultType, int> pointsByOutcome, IEnumerable<IStandingColumn> columns, StandingComparer comparer) : ValueObject
{
    public static Dictionary<MatchResultType, int> DefaultPoints => new()
    {
            { MatchResultType.Win, 3 },
            { MatchResultType.Draw, 1 },
            { MatchResultType.Loss, 0 },
            { MatchResultType.Withdraw, -1 },
    };

    public static readonly StandingRuleSet Default = new(DefaultPoints, DefaultColumns, StandingComparer.Default);

    public static IReadOnlyList<IStandingColumn> DefaultColumns => [.. Enum.GetValues<StandingColumnType>().Select(StandingColumn.Create)];

    public StandingComparer Comparer { get; } = comparer;

    public IReadOnlyDictionary<MatchResultType, int> PointsByOutcome { get; } = new Dictionary<MatchResultType, int>(pointsByOutcome);

    public IReadOnlyList<IStandingColumn> Columns { get; } = columns.ToList().AsReadOnly();

    public int GetPoints(MatchResultType result) => PointsByOutcome.GetValueOrDefault(result);

    public IStandingColumn? GetColumn(string column) => Columns.FirstOrDefault(x => string.Equals(x.Key, column, StringComparison.OrdinalIgnoreCase));

    public int ComputePoints(TeamReference team, IEnumerable<IMatch> matches) => matches.Sum(m => GetPoints(m.GetResultOf(team)));
}
