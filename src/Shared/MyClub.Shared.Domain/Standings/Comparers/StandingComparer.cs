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
using MyClub.Shared.Domain.Matchs;
using MyClub.Shared.Domain.Standings.Rules;
using MyNet.Utilities;

namespace MyClub.Shared.Domain.Standings.Comparers;

public class StandingComparer(IEnumerable<IStandingComparer> comparers) : StandingRowComparer, IStandingContextualComparer, IEnumerable<IStandingComparer>
{
    private readonly List<IStandingComparer> _comparers = [.. comparers];

    public static IDictionary<string, Func<IStandingComparer>> AllAvailableComparers => new Dictionary<string, Func<IStandingComparer>>
        {
            { nameof(StandingRowByPointsComparer), () => new StandingRowByPointsComparer() },
            { nameof(StandingRowByHeadToHeadComparer), () => new StandingRowByHeadToHeadComparer() },
            { nameof(StandingRowByGoalsDifferenceComparer), () => new StandingRowByGoalsDifferenceComparer() },
            { nameof(StandingRowByGoalsForComparer), () => new StandingRowByGoalsForComparer() },
            { nameof(StandingRowByGoalsAgainstComparer), () => new StandingRowByGoalsAgainstComparer() },
            { nameof(StandingRowByGamesWonComparer), () => new StandingRowByGamesWonComparer() },
            { nameof(StandingRowByGamesWonAfterShootoutsComparer), () => new StandingRowByGamesWonAfterShootoutsComparer() },
            { nameof(StandingRowByGamesLostComparer), () => new StandingRowByGamesLostComparer() },
            { nameof(StandingRowByGamesLostAfterShootoutsComparer), () => new StandingRowByGamesLostAfterShootoutsComparer() },
            { nameof(StandingRowByGamesWithdrawnComparer), () => new StandingRowByGamesWithdrawnComparer() },
            { nameof(StandingRowByPenaltyPointsComparer), () => new StandingRowByPenaltyPointsComparer() },
        };

    public static StandingComparer Default => new StandingComparerBuilder().ThenByPoints()
                                                                           .ThenBy(StandingColumnType.GoalsDifference)
                                                                           .ThenByHeadToHead()
                                                                           .ThenBy(StandingColumnType.GoalsFor)
                                                                           .Build();

    public void SetContext(IEnumerable<IMatch> matches, StandingRuleSet rules) => _comparers.OfType<IStandingContextualComparer>().ForEach(comparer => comparer.SetContext(matches, rules));

    protected override int CompareTo(IStandingRow x, IStandingRow y)
    {
        foreach (var rule in _comparers)
        {
            var ruleResult = rule.Compare(x, y);

            if (ruleResult != 0) return ruleResult;
        }

        return 0;
    }

    public IEnumerator<IStandingComparer> GetEnumerator() => _comparers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public abstract class StandingRowComparer : IStandingComparer
{
    public int Compare(IStandingRow? x, IStandingRow? y) => x == null || y == null || x == y ? 0 : CompareTo(x, y);

    protected abstract int CompareTo(IStandingRow x, IStandingRow y);
}

public class StandingRowByComparableComparer(Func<IStandingRow, IComparable?> compareExpression, bool ascending = false) : StandingRowComparer
{
    private readonly Func<IStandingRow, IComparable?> _compareExpression = compareExpression;
    private readonly bool _ascending = ascending;

    protected override int CompareTo(IStandingRow x, IStandingRow y)
    {
        var compareX = _compareExpression(x);
        var compareY = _compareExpression(y);

        var ascendingModifier = _ascending ? 1 : -1;
        return compareX == default && compareY == default ? 0 : compareX == default ? -ascendingModifier : compareY == default ? ascendingModifier : compareX.CompareTo(compareY) * ascendingModifier;
    }
}

public class StandingRowByColumnComparer : StandingRowByComparableComparer
{
    public StandingRowByColumnComparer(string column, bool ascending = false)
        : base(x => x.Get<IComparable>(column), ascending) { }

    public StandingRowByColumnComparer(StandingColumnType column, bool ascending = false)
        : base(x => x.Get(column), ascending) { }
}

public class StandingRowByPointsComparer() : StandingRowByComparableComparer(x => x.Points);

public class StandingRowByPenaltyPointsComparer() : StandingRowByComparableComparer(x => x.PenaltyPoints);

public class StandingRowByGoalsForComparer() : StandingRowByColumnComparer(StandingColumnType.GoalsFor);

public class StandingRowByGoalsAgainstComparer() : StandingRowByColumnComparer(StandingColumnType.GoalsAgainst, true);

public class StandingRowByGoalsDifferenceComparer() : StandingRowByColumnComparer(StandingColumnType.GoalsDifference);

public class StandingRowByGamesWonComparer() : StandingRowByColumnComparer(StandingColumnType.GamesWon);

public class StandingRowByGamesWonAfterShootoutsComparer() : StandingRowByColumnComparer(StandingColumnType.GamesWonAfterShootouts);

public class StandingRowByGamesLostComparer() : StandingRowByColumnComparer(StandingColumnType.GamesLost);

public class StandingRowByGamesLostAfterShootoutsComparer() : StandingRowByColumnComparer(StandingColumnType.GamesLostAfterShootouts);

public class StandingRowByGamesWithdrawnComparer() : StandingRowByColumnComparer(StandingColumnType.GamesWithdrawn);

public class StandingRowByHeadToHeadComparer() : StandingRowComparer, IStandingContextualComparer
{
    private IEnumerable<IMatch>? _matches;
    private StandingRuleSet? _rules;

    public void SetContext(IEnumerable<IMatch> matches, StandingRuleSet rules)
    {
        _matches = matches;
        _rules = rules;
    }

    protected override int CompareTo(IStandingRow x, IStandingRow y)
    {
        if (_matches is null || _rules is null || x.Team == y.Team)
            return 0;

        var matches = _matches.Where(m => m.HasResult(x.Team) && m.HasResult(y.Team)).ToList();
        var teams = _matches.SelectMany(x => x.GetTeams()).Distinct().ToList();

        if (matches.Count == 0 || teams.Count == 0)
            return 0;

        var rules = new StandingRuleSet(Enum.GetValues<MatchResultType>().ToDictionary(x => x, _rules.GetPoints), _rules.Columns, new StandingComparer([.. _rules.Comparer.Where(x => x is not StandingRowByHeadToHeadComparer)]));
        var standing = new Standing(teams, rules);
        standing.ComputeAll(_matches);

        return standing.GetRank(x.Team).CompareTo(standing.GetRank(y.Team));
    }
}
