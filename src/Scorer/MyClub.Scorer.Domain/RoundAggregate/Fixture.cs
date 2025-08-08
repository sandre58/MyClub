// -----------------------------------------------------------------------
// <copyright file="Fixture.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.RoundAggregate;

/// <summary>
/// Represents a fixture entity that defines a matchup between two teams in a tournament round.
/// A fixture groups together the matches that will be played between two specific teams,
/// which can be one match, a home-and-away series, or a best-of-X series depending on the round format.
/// </summary>
public class Fixture : AuditableEntity<FixtureId>
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private Fixture()
    {
        Team1 = null!;
        Team2 = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Fixture"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the fixture.</param>
    /// <param name="team1">The first team in the fixture matchup.</param>
    /// <param name="team2">The second team in the fixture matchup.</param>
    private Fixture(FixtureId id, TeamReference team1, TeamReference team2)
        : base(id)
    {
        Team1 = team1;
        Team2 = team2;
    }

    /// <summary>
    /// Creates a new fixture between two teams.
    /// </summary>
    /// <param name="team1">The first team in the fixture matchup.</param>
    /// <param name="team2">The second team in the fixture matchup.</param>
    /// <returns>A new <see cref="Fixture"/> instance.</returns>
    /// <remarks>
    /// Both teams can be concrete team references or virtual team references.
    /// Virtual references are useful for setting up tournament brackets before all qualifying teams are known.
    /// </remarks>
    public static Fixture Create(TeamReference team1, TeamReference team2) => new(FixtureId.New(), team1, team2);

    /// <summary>
    /// Gets the first team in the fixture matchup.
    /// This can be a concrete team or a virtual team reference.
    /// </summary>
    public TeamReference Team1 { get; }

    /// <summary>
    /// Gets the second team in the fixture matchup.
    /// This can be a concrete team or a virtual team reference.
    /// </summary>
    public TeamReference Team2 { get; }

    /// <summary>
    /// Determines whether a specific team participates in this fixture.
    /// </summary>
    /// <param name="team">The team to check for participation.</param>
    /// <returns>True if the team participates in this fixture; otherwise, false.</returns>
    public bool Participate(TeamReference team) => GetTeams().Contains(team);

    /// <summary>
    /// Gets all teams participating in this fixture.
    /// </summary>
    /// <returns>An enumerable collection containing both teams in the fixture (duplicates removed).</returns>
    /// <remarks>
    /// The Distinct() call handles the edge case where the same team might be referenced twice,
    /// though this would be unusual in normal tournament scenarios.
    /// </remarks>
    public IEnumerable<TeamReference> GetTeams() => new List<TeamReference> { Team1, Team2 }.Distinct();

    /// <summary>
    /// Returns a string representation of the fixture in the format "Team1 vs Team2".
    /// </summary>
    /// <returns>A formatted string representing the fixture matchup.</returns>
    public override string ToString() => $"{Team1} vs {Team2}";
}
