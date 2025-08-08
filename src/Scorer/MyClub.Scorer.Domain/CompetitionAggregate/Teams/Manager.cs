// -----------------------------------------------------------------------
// <copyright file="Manager.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Shared.Domain.Persons;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Teams;

/// <summary>
/// Represents a manager (coach or staff member) entity within a team's organization.
/// A manager extends the shared Person domain model to represent coaching staff, technical directors,
/// and other management personnel responsible for team operations and strategy.
/// </summary>
public class Manager : Person<ManagerId>
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private Manager() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Manager"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the manager.</param>
    /// <param name="firstName">The first name of the manager.</param>
    /// <param name="lastName">The last name of the manager.</param>
    private Manager(ManagerId id, string firstName, string lastName)
        : base(id, firstName, lastName) { }

    /// <summary>
    /// Creates a new manager with the specified name.
    /// </summary>
    /// <param name="firstName">The first name of the manager.</param>
    /// <param name="lastName">The last name of the manager.</param>
    /// <returns>A new <see cref="Manager"/> instance with a generated unique identifier.</returns>
    public static Manager Create(string firstName, string lastName) => new(ManagerId.New(), firstName, lastName);
}
