// -----------------------------------------------------------------------
// <copyright file="Team.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Referential.Domain.Aggregates.TeamAggregate;

public class Team : TeamBase<TeamId>, IAggregateRoot
{
    // <remarks>Used by EF Core</remarks>
    private Team()
        : base() { }

    private Team(TeamId id, string name, string? shortName = null)
        : base(id, name, shortName)
    {
    }

    public static Team Create(string name, string? shortName = null) => new(TeamId.New(), name, shortName);
}
