// -----------------------------------------------------------------------
// <copyright file="StandingColumn.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matchs;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Shared.Domain.Standings.Rules;

public static class StandingColumn
{
    public static IStandingColumn Create(StandingColumnType rankingColumn)
    => rankingColumn switch
    {
        StandingColumnType.GamesPlayed => new PlayedColumn(),
        StandingColumnType.GamesWon => new GamesWonColumn(),
        StandingColumnType.GamesWonAfterShootouts => new GamesWonAfterShootoutsColumn(),
        StandingColumnType.GamesDrawn => new GamesDrawnColumn(),
        StandingColumnType.GamesLost => new GamesLostColumn(),
        StandingColumnType.GamesLostAfterShootouts => new GamesLostAfterShootoutsColumn(),
        StandingColumnType.GamesWithdrawn => new GamesWithdrawnColumn(),
        StandingColumnType.GoalsFor => new GoalsForColumn(),
        StandingColumnType.GoalsAgainst => new GoalsAgainstColumn(),
        StandingColumnType.GoalsDifference => new GoalsDifferenceColumn(),
        _ => throw new InvalidOperationException("Invalid RankingColumn"),
    };
}

public class StandingColumn<T>(string key, Func<T, TeamReference, IMatch, T> incrementalCalculator, Func<TeamReference, IEnumerable<IMatch>, T> batchCalculator, T defaultValue) : IStandingColumn<T>
    where T : notnull
{
    public string Key { get; } = key;

    public T DefaultValue { get; } = defaultValue;

    object IStandingColumn.DefaultValue => DefaultValue;

    public T ComputeIncremental(T previousValue, TeamReference team, IMatch match) => incrementalCalculator.Invoke(previousValue, team, match);

    object IStandingColumn.ComputeIncremental(object previousValue, TeamReference team, IMatch match) => incrementalCalculator.Invoke((T)previousValue, team, match)!;

    public T ComputeBatch(TeamReference team, IEnumerable<IMatch> matches) => batchCalculator.Invoke(team, matches);

    object IStandingColumn.ComputeBatch(TeamReference team, IEnumerable<IMatch> matches) => batchCalculator.Invoke(team, matches)!;
}

public class StandingIntColumn(string key, Func<TeamReference, IEnumerable<IMatch>, int> calculator, int defaultValue = 0) : StandingColumn<int>(key, (previousValue, team, match) => previousValue + calculator.Invoke(team, [match]), calculator, defaultValue);

public class StandingDoubleColumn(string key, Func<TeamReference, IEnumerable<IMatch>, double> calculator, double defaultValue = 0.0) : StandingColumn<double>(key, (previousValue, team, match) => previousValue + calculator.Invoke(team, [match]), calculator, defaultValue);

public class PlayedColumn() : StandingIntColumn(nameof(StandingColumnType.GamesPlayed), (team, matches) => matches.Count(x => x.HasResult() && x.Participate(team)));

public class GamesWonColumn() : StandingIntColumn(nameof(StandingColumnType.GamesWon), (team, matches) => matches.Count(x => x.GetResultOf(team) is MatchResultType.Win));

public class GamesWonAfterShootoutsColumn() : StandingIntColumn(nameof(StandingColumnType.GamesWonAfterShootouts), (team, matches) => matches.Count(x => x.GetResultOf(team) is MatchResultType.WinAfterShootouts));

public class GamesDrawnColumn() : StandingIntColumn(nameof(StandingColumnType.GamesDrawn), (team, matches) => matches.Count(x => x.GetResultOf(team) is MatchResultType.Draw));

public class GamesLostColumn() : StandingIntColumn(nameof(StandingColumnType.GamesLost), (team, matches) => matches.Count(x => x.GetResultOf(team) is MatchResultType.Loss));

public class GamesLostAfterShootoutsColumn() : StandingIntColumn(nameof(StandingColumnType.GamesLostAfterShootouts), (team, matches) => matches.Count(x => x.GetResultOf(team) is MatchResultType.LossAfterShootouts));

public class GamesWithdrawnColumn() : StandingIntColumn(nameof(StandingColumnType.GamesWithdrawn), (team, matches) => matches.Count(x => x.GetResultOf(team) is MatchResultType.Withdraw));

public class GoalsForColumn() : StandingIntColumn(nameof(StandingColumnType.GoalsFor), (team, matches) => matches.Sum(x => x.GoalsFor(team)));

public class GoalsAgainstColumn() : StandingIntColumn(nameof(StandingColumnType.GoalsAgainst), (team, matches) => matches.Sum(x => x.GoalsAgainst(team)));

public class GoalsDifferenceColumn() : StandingIntColumn(nameof(StandingColumnType.GoalsDifference), (team, matches) => matches.Sum(x => x.GoalsFor(team) - x.GoalsAgainst(team)));
