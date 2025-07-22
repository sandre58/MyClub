// -----------------------------------------------------------------------
// <copyright file="Stadium.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Stadiums;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Referential.Domain.Aggregates.StadiumAggregate;

public class Stadium : StadiumBase<StadiumId>, IAggregateRoot
{
    // <remarks>Used by EF Core</remarks>
    private Stadium()
        : base() { }

    private Stadium(StadiumId id, string name, Ground ground)
        : base(id, name, ground) { }

    public static Stadium Create(string name, Ground ground) => new(StadiumId.New(), name, ground);
}
