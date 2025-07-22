// -----------------------------------------------------------------------
// <copyright file="Manager.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Domain.Persons;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Teams;

public class Manager : Person<ManagerId>
{
    // <remarks>Used by EF Core</remarks>
    private Manager()
        : base() { }

    private Manager(ManagerId id, string firstName, string lastName)
        : base(id, firstName, lastName) { }

    public static Manager Create(string firstName, string lastName) => new(ManagerId.New(), firstName, lastName);
}
