// -----------------------------------------------------------------------
// <copyright file="Player.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Domain.Persons;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Referential.Domain.Aggregates.PlayerAggregate;

public class Player : Person<PlayerId>, IAggregateRoot
{
    // <remarks>Used by EF Core</remarks>
    private Player()
        : base() { }

    private Player(PlayerId id, string firstName, string lastName)
        : base(id, firstName, lastName) { }

    public static Player Create(string firstName, string lastName) => new(PlayerId.New(), firstName, lastName);
}
