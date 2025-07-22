// -----------------------------------------------------------------------
// <copyright file="DummyStandingRow.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matchs;
using MyClub.Shared.Domain.Standings;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Shared.Tests.Standings;

// Dummy StandingRow for testing
internal sealed class DummyStandingRow(TeamReference team,
                                       int points = 0,
                                       int penaltyPoints = 0,
                                       int goalsFor = 0,
                                       int goalsAgainst = 0,
                                       int goalsDifference = 0,
                                       int gamesWon = 0,
                                       int gamesLost = 0,
                                       int gamesPlayed = 0,
                                       int gamesWonAfterShootouts = 0,
                                       int gamesDrawn = 0,
                                       int gamesLostAfterShootouts = 0,
                                       int gamesWithdrawn = 0) : IStandingRow
{
    public TeamReference Team { get; } = team;

    public int Points { get; } = points;

    public int PenaltyPoints { get; } = penaltyPoints;

    public int GoalsFor { get; } = goalsFor;

    public int GoalsAgainst { get; } = goalsAgainst;

    public int GoalsDifference { get; } = goalsDifference;

    public int GamesWon { get; } = gamesWon;

    public int GamesLost { get; } = gamesLost;

    public int GamesPlayed { get; } = gamesPlayed;

    public int GamesWonAfterShootouts { get; } = gamesWonAfterShootouts;

    public int GamesDrawn { get; } = gamesDrawn;

    public int GamesLostAfterShootouts { get; } = gamesLostAfterShootouts;

    public int GamesWithdrawn { get; } = gamesWithdrawn;

    public object? Get(string column) => column switch
    {
        nameof(StandingColumnType.GoalsFor) => GoalsFor,
        nameof(StandingColumnType.GoalsAgainst) => GoalsAgainst,
        nameof(StandingColumnType.GoalsDifference) => GoalsDifference,
        nameof(StandingColumnType.GamesWon) => GamesWon,
        nameof(StandingColumnType.GamesLost) => GamesLost,
        nameof(StandingColumnType.GamesPlayed) => GamesPlayed,
        nameof(StandingColumnType.GamesWonAfterShootouts) => GamesWonAfterShootouts,
        nameof(StandingColumnType.GamesDrawn) => GamesDrawn,
        nameof(StandingColumnType.GamesLostAfterShootouts) => GamesLostAfterShootouts,
        nameof(StandingColumnType.GamesWithdrawn) => (object)GamesWithdrawn,
        _ => default!
    };

    public T? Get<T>(string column) => (T)Get(column.ToString())!;

    public int Get(StandingColumnType column) => Get<int>(column.ToString())!;

    public void ApplyMatch(IMatch match, StandingRuleSet rules) { }

    public void Compute(IEnumerable<IMatch> matches, StandingRuleSet rules) { }
}
