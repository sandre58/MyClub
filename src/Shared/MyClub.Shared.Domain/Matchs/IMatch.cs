// -----------------------------------------------------------------------
// <copyright file="IMatch.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Stadiums;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Shared.Domain.Matchs;

public interface IMatch
{
    TeamReference HomeTeamReference { get; }

    TeamReference AwayTeamReference { get; }

    DateTime Date { get; }

    StadiumId? StadiumId { get; }

    bool Participate(TeamReference team);

    bool IsHomeTeam(TeamReference team);

    bool IsAwayTeam(TeamReference team);

    IReadOnlyCollection<TeamReference> GetTeams();

    bool HasResult();

    bool HasResult(TeamReference team);

    bool IsDraw();

    MatchResultType GetResultOf(TeamReference team);

    MatchOutcome GetOutcomeOf(TeamReference team);

    TeamReference? GetWinner();

    TeamReference? GetLooser();

    bool IsWonBy(TeamReference team);

    bool IsLostBy(TeamReference team);

    bool IsWithdrawn(TeamReference team);

    int GoalsFor(TeamReference team);

    int GoalsAgainst(TeamReference team);

    int ShootoutFor(TeamReference team);

    int ShootoutAgainst(TeamReference team);
}
