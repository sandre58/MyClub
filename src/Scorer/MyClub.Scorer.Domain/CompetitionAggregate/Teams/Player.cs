// -----------------------------------------------------------------------
// <copyright file="Player.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Shared.Domain.Persons;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Teams;

/// <summary>
/// Represents a football player entity within a team's roster.
/// A player extends the shared Person domain model with football-specific capabilities
/// and can participate in matches, score goals, receive cards, and contribute to team statistics.
/// </summary>
public class Player : Person<PlayerId>
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private Player() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Player"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the player.</param>
    /// <param name="firstName">The first name of the player.</param>
    /// <param name="lastName">The last name of the player.</param>
    private Player(PlayerId id, string firstName, string lastName)
        : base(id, firstName, lastName) { }

    /// <summary>
    /// Creates a new player with the specified name.
    /// </summary>
    /// <param name="firstName">The first name of the player.</param>
    /// <param name="lastName">The last name of the player.</param>
    /// <returns>A new <see cref="Player"/> instance with a generated unique identifier.</returns>
    public static Player Create(string firstName, string lastName) => new(PlayerId.New(), firstName, lastName);
}
