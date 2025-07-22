// -----------------------------------------------------------------------
// <copyright file="TeamId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Shared.Domain.Teams;

public sealed record TeamId(Guid Value) : EntityId<TeamId>(Value)
{
    public static implicit operator ConcreteTeamReference(TeamId id) => ToConcreteTeamReference(id);

    public static implicit operator TeamId(ConcreteTeamReference value) => ToTeamId(value);

    public static ConcreteTeamReference ToConcreteTeamReference(TeamId id) => new(id);

    public static TeamId ToTeamId(ConcreteTeamReference value) => value.Id;

    public TeamReference ToReference() => ToConcreteTeamReference(this);
}
