// -----------------------------------------------------------------------
// <copyright file="Manager.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Shared.Domain.Persons;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Referential.Domain.ManagerAggregate;

/// <summary>
/// Represents a football manager or coaching staff member as a reference entity within the MyClub ecosystem,
/// providing core manager identification and personal information for use across multiple modules.
/// This aggregate root maintains manager reference data that can be shared between different
/// functional areas such as team management, match coordination, and staff administration.
/// </summary>
public class Manager : Person<ManagerId>, IAggregateRoot
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private Manager() { }

    private Manager(ManagerId id, string firstName, string lastName)
        : base(id, firstName, lastName) { }

    /// <summary>
    /// Creates a new Manager instance with the specified first and last names,
    /// generating a new unique identifier and ensuring proper entity initialization.
    /// </summary>
    /// <param name="firstName">The first name of the manager. Cannot be null or empty.</param>
    /// <param name="lastName">The last name of the manager. Cannot be null or empty.</param>
    /// <returns>A new Manager instance with a generated unique identifier.</returns>
    public static Manager Create(string firstName, string lastName) => new(ManagerId.New(), firstName, lastName);
}
