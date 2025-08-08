// -----------------------------------------------------------------------
// <copyright file="IMatchdayScheduleStrategy.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Scorer.Domain.MatchdayAggregate.Services;

/// <summary>
/// Interface for strategies that generate matchday schedules for team competitions.
/// Implementations define how teams are paired and distributed across matchdays
/// to create fair and balanced competition schedules.
/// </summary>
public interface IMatchdayScheduleStrategy
{
    /// <summary>
    /// Generates a complete schedule of matchdays for the given teams.
    /// Each matchday contains a set of fixtures that can be played simultaneously.
    /// </summary>
    /// <param name="teams">The teams that will participate in the competition.</param>
    /// <returns>
    /// An enumerable sequence of matchday definitions, each containing fixtures for that round.
    /// The order represents the chronological sequence of matchdays.
    /// </returns>
    IEnumerable<MatchdayDefinition> GenerateSchedule(IEnumerable<TeamReference> teams);

    /// <summary>
    /// Calculates the total number of matchdays required for the given number of teams.
    /// This allows for advance planning and calendar allocation without generating the full schedule.
    /// </summary>
    /// <param name="teamCount">The number of teams in the competition.</param>
    /// <returns>The total number of matchdays needed to complete the competition format.</returns>
    int GetMatchdayCount(int teamCount);

    /// <summary>
    /// Calculates the maximum number of matches that can be played simultaneously in any single matchday.
    /// This represents the peak venue and scheduling requirements for the competition.
    /// </summary>
    /// <param name="teamCount">The number of teams in the competition.</param>
    /// <returns>The maximum number of concurrent matches in any matchday.</returns>
    int GetMaxMatchesPerMatchday(int teamCount);
}

/// <summary>
/// Represents a definition of a matchday containing all fixtures to be played during that round.
/// This is a lightweight structure used for schedule generation and planning.
/// </summary>
/// <param name="Fixtures">The collection of fixture definitions for this matchday.</param>
public record MatchdayDefinition(IReadOnlyCollection<FixtureDefinition> Fixtures);

/// <summary>
/// Represents a fixture definition specifying which teams will play against each other.
/// This defines the pairing and venue assignment for a single match within a matchday.
/// </summary>
/// <param name="HomeTeam">The team reference for the team playing at home.</param>
/// <param name="AwayTeam">The team reference for the team playing away.</param>
public record FixtureDefinition(TeamReference HomeTeam, TeamReference AwayTeam);
