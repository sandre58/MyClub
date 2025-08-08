// -----------------------------------------------------------------------
// <copyright file="Team.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Referential.Domain.TeamAggregate;

/// <summary>
/// Represents a football team as a reference entity within the MyClub ecosystem,
/// providing core team identification and display information for use across multiple modules.
/// This aggregate root maintains team reference data that can be shared between different
/// functional areas such as competition management, player administration, and match organization.
/// </summary>
public class Team : TeamBase<TeamId>, IAggregateRoot
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private Team() { }

    private Team(TeamId id, string name, string? shortName = null)
        : base(id, name, shortName)
    {
    }

    /// <summary>
    /// Creates a new Team instance with the specified name and optional short name,
    /// generating a new unique identifier and ensuring proper entity initialization.
    /// </summary>
    /// <param name="name">The full name of the team. Cannot be null or empty.</param>
    /// <param name="shortName">The optional short name or abbreviation for the team.</param>
    /// <returns>A new Team instance with a generated unique identifier.</returns>
    public static Team Create(string name, string? shortName = null) => new(TeamId.New(), name, shortName);
}
