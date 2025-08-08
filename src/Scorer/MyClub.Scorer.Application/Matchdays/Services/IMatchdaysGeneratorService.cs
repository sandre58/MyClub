// -----------------------------------------------------------------------
// <copyright file="IMatchdaysGeneratorService.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Scorer.Application.Matchdays.Services;

/// <summary>
/// Service interface for generating complete matchday schedules for football competitions.
/// This service orchestrates the domain scheduling strategies to create comprehensive
/// fixture lists with properly organized matchdays and matches.
/// </summary>
public interface IMatchdaysGeneratorService
{
    /// <summary>
    /// Generates a complete set of matchdays with associated matches for the specified teams.
    /// This method creates a full competition schedule using appropriate domain strategies
    /// and converts the results into ready-to-use matchday and match entities.
    /// </summary>
    /// <param name="teams">
    /// The collection of team references participating in the competition.
    /// Can include both concrete teams and virtual team references for complex tournaments.
    /// </param>
    /// <returns>
    /// A read-only collection of Matchday entities, each containing the matches to be played
    /// during that specific round of the competition.
    /// </returns>
    IReadOnlyCollection<Matchday> GenerateMatchdays(IReadOnlyCollection<TeamReference> teams);
}

/// <summary>
/// Represents a generated matchday with its associated match collection.
/// This record provides a convenient way to package matchday entities with their
/// related matches for bulk operations and schedule management.
/// </summary>
/// <param name="Matchday">The matchday entity containing scheduling information and metadata.</param>
/// <param name="Matches">The collection of matches scheduled to be played during this matchday.</param>
public record GeneraredMatchday(Matchday Matchday, IReadOnlyCollection<Match> Matches);
