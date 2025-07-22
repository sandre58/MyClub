// -----------------------------------------------------------------------
// <copyright file="TeamBase.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Domain.Stadiums;
using MyClub.Shared.Domain.ValueObjects;
using MyClub.Shared.Kernel.Primitives;
using MyNet.Utilities.Geography;

namespace MyClub.Shared.Domain.Teams;

public abstract class TeamBase<TId> : AuditableEntity<TId>, ITeam
    where TId : EntityId<TId>
{
    // <remarks>Used by EF Core</remarks>
    protected TeamBase()
        : base() => DisplayName = null!;

    protected TeamBase(TId id, string name, string? shortName = null)
        : base(id) => DisplayName = new(name, shortName);

    public DisplayName DisplayName { get; set; }

    public byte[]? Logo { get; set; }

    public Country? Country { get; set; }

    public StadiumId? StadiumId { get; set; }

    public string? HomeColor { get; set; }

    public string? AwayColor { get; set; }

    public override string ToString() => DisplayName;

    public int CompareTo(ITeam? other) => DisplayName.CompareTo(other?.DisplayName);

    public override int CompareTo(Entity<TId>? other) => other is not ITeam entity ? -1 : CompareTo(entity);

    public bool IsSimilar(ITeam? obj) => DisplayName.IsSimilar(obj?.DisplayName);

    public bool IsSimilar(object? obj) => obj is ITeam person && IsSimilar(person);
}
