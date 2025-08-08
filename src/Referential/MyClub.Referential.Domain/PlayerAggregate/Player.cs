// -----------------------------------------------------------------------
// <copyright file="Player.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Shared.Domain.Persons;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Referential.Domain.PlayerAggregate;

/// <summary>
/// Represents a football player as a reference entity within the MyClub ecosystem,
/// providing core player identification and personal information for use across multiple modules.
/// This aggregate root maintains player reference data that can be shared between different
/// functional areas such as match events, team rosters, and player statistics.
/// </summary>
public class Player : Person<PlayerId>, IAggregateRoot
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private Player() { }

    private Player(PlayerId id, string firstName, string lastName)
        : base(id, firstName, lastName) { }

    /// <summary>
    /// Creates a new Player instance with the specified first and last names,
    /// generating a new unique identifier and ensuring proper entity initialization.
    /// </summary>
    /// <param name="firstName">The first name of the player. Cannot be null or empty.</param>
    /// <param name="lastName">The last name of the player. Cannot be null or empty.</param>
    /// <returns>A new Player instance with a generated unique identifier.</returns>
    public static Player Create(string firstName, string lastName) => new(PlayerId.New(), firstName, lastName);
}
