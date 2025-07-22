// -----------------------------------------------------------------------
// <copyright file="Manager.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Domain.Persons;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Referential.Domain.Aggregates.ManagerAggregate;

public class Manager : Person<ManagerId>, IAggregateRoot
{
    // <remarks>Used by EF Core</remarks>
    private Manager()
        : base() { }

    private Manager(ManagerId id, string firstName, string lastName)
        : base(id, firstName, lastName) { }

    public static Manager Create(string firstName, string lastName) => new(ManagerId.New(), firstName, lastName);
}
