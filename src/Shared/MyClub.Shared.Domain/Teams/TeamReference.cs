// -----------------------------------------------------------------------
// <copyright file="TeamReference.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Shared.Domain.Teams;

public abstract record TeamReference;

public record ConcreteTeamReference(TeamId Id) : TeamReference
{
    public static implicit operator ConcreteTeamReference(TeamId id) => ToConcreteTeamReference(id);

    public static implicit operator TeamId(ConcreteTeamReference value) => ToTeamId(value);

    public static ConcreteTeamReference ToConcreteTeamReference(TeamId id) => new(id);

    public static TeamId ToTeamId(ConcreteTeamReference value) => value.Id;
}
