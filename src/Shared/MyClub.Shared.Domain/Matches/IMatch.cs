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

namespace MyClub.Shared.Domain.Matches;

/// <summary>
/// Interface representing a match in the sports management system.
/// Defines the contract for match entities across different modules, providing
/// essential match information and behavior for team participation, results, and statistics.
/// </summary>
public interface IMatch
{
    /// <summary>
    /// Gets the reference to the home team in this match.
    /// This can be either a concrete team reference (with a specific TeamId) or a virtual team reference.
    /// </summary>
    TeamReference HomeTeamReference { get; }

    /// <summary>
    /// Gets the reference to the away team in this match.
    /// This can be either a concrete team reference (with a specific TeamId) or a virtual team reference.
    /// </summary>
    TeamReference AwayTeamReference { get; }

    /// <summary>
    /// Gets the scheduled date and time when this match is to be played or was played.
    /// </summary>
    DateTime Date { get; }

    /// <summary>
    /// Gets the identifier of the stadium where this match is to be played or was played.
    /// This can be null if no specific stadium has been assigned.
    /// </summary>
    StadiumId? StadiumId { get; }

    /// <summary>
    /// Determines whether the specified team participates in this match (either as home or away team).
    /// </summary>
    /// <param name="team">The team reference to check for participation.</param>
    /// <returns>true if the team participates in this match; otherwise, false.</returns>
    bool Participate(TeamReference team);

    /// <summary>
    /// Determines whether the specified team is the home team in this match.
    /// </summary>
    /// <param name="team">The team reference to check.</param>
    /// <returns>true if the team is the home team; otherwise, false.</returns>
    bool IsHomeTeam(TeamReference team);

    /// <summary>
    /// Determines whether the specified team is the away team in this match.
    /// </summary>
    /// <param name="team">The team reference to check.</param>
    /// <returns>true if the team is the away team; otherwise, false.</returns>
    bool IsAwayTeam(TeamReference team);

    /// <summary>
    /// Gets all teams participating in this match.
    /// This typically returns a collection containing the home team and away team references.
    /// </summary>
    /// <returns>A read-only collection of team references participating in this match.</returns>
    IReadOnlyCollection<TeamReference> GetTeams();

    /// <summary>
    /// Determines whether this match has a result (i.e., has been played and a result recorded).
    /// </summary>
    /// <returns>true if the match has a result; otherwise, false.</returns>
    bool HasResult();

    /// <summary>
    /// Determines whether a result has been recorded for the specified team in this match.
    /// </summary>
    /// <param name="team">The team reference to check for a result.</param>
    /// <returns>true if a result exists for the specified team; otherwise, false.</returns>
    bool HasResult(TeamReference team);

    /// <summary>
    /// Determines whether this match ended in a draw.
    /// </summary>
    /// <returns>true if the match was a draw; otherwise, false.</returns>
    bool IsDraw();

    /// <summary>
    /// Gets the result type (Win, Draw, Loss) for the specified team in this match.
    /// </summary>
    /// <param name="team">The team reference to get the result for.</param>
    /// <returns>The result type for the specified team.</returns>
    MatchResultType GetResultOf(TeamReference team);

    /// <summary>
    /// Gets the outcome (Win, Draw, Loss, Withdrawn) for the specified team in this match.
    /// This is similar to GetResultOf but may include additional outcomes like withdrawals.
    /// </summary>
    /// <param name="team">The team reference to get the outcome for.</param>
    /// <returns>The outcome for the specified team.</returns>
    MatchOutcome GetOutcomeOf(TeamReference team);

    /// <summary>
    /// Gets the team reference of the winner of this match.
    /// </summary>
    /// <returns>The team reference of the winner, or null if the match was a draw or has no result.</returns>
    TeamReference? GetWinner();

    /// <summary>
    /// Gets the team reference of the loser of this match.
    /// </summary>
    /// <returns>The team reference of the loser, or null if the match was a draw or has no result.</returns>
    TeamReference? GetLooser();

    /// <summary>
    /// Determines whether the specified team won this match.
    /// </summary>
    /// <param name="team">The team reference to check.</param>
    /// <returns>true if the team won the match; otherwise, false.</returns>
    bool IsWonBy(TeamReference team);

    /// <summary>
    /// Determines whether the specified team lost this match.
    /// </summary>
    /// <param name="team">The team reference to check.</param>
    /// <returns>true if the team lost the match; otherwise, false.</returns>
    bool IsLostBy(TeamReference team);

    /// <summary>
    /// Determines whether the specified team withdrew from this match.
    /// </summary>
    /// <param name="team">The team reference to check.</param>
    /// <returns>true if the team withdrew from the match; otherwise, false.</returns>
    bool IsWithdrawn(TeamReference team);

    /// <summary>
    /// Gets the number of goals scored by the specified team in this match.
    /// </summary>
    /// <param name="team">The team reference to get goals for.</param>
    /// <returns>The number of goals scored by the team.</returns>
    int GoalsFor(TeamReference team);

    /// <summary>
    /// Gets the number of goals scored against the specified team in this match.
    /// </summary>
    /// <param name="team">The team reference to get goals against for.</param>
    /// <returns>The number of goals scored against the team.</returns>
    int GoalsAgainst(TeamReference team);

    /// <summary>
    /// Gets the number of successful penalty shots by the specified team in a shootout.
    /// This is relevant for matches that are decided by penalty shootouts.
    /// </summary>
    /// <param name="team">The team reference to get shootout goals for.</param>
    /// <returns>The number of successful penalty shots by the team.</returns>
    int ShootoutFor(TeamReference team);

    /// <summary>
    /// Gets the number of penalty shots scored against the specified team in a shootout.
    /// This is relevant for matches that are decided by penalty shootouts.
    /// </summary>
    /// <param name="team">The team reference to get shootout goals against for.</param>
    /// <returns>The number of penalty shots scored against the team.</returns>
    int ShootoutAgainst(TeamReference team);
}
