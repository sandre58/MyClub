// -----------------------------------------------------------------------
// <copyright file="StadiumBase.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.ValueObjects;
using MyClub.Shared.Kernel.Primitives;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Shared.Domain.Stadiums;

public abstract class StadiumBase<TId> : AuditableEntity<TId>, IStadium
    where TId : EntityId<TId>
{
    // <remarks>Used by EF Core</remarks>
    protected StadiumBase()
        : base() => DisplayName = null!;

    protected StadiumBase(TId id, string name, Ground ground)
        : base(id)
    {
        DisplayName = new(name);
        Ground = ground;
    }

    public DisplayName DisplayName { get; set; }

    public Ground Ground { get; set; }

    public Address? Address { get; set; }

    public override string ToString() => string.Join(", ", new[] { DisplayName.Name, Address?.City }.NotNull());

    public int CompareTo(IStadium? other) => DisplayName.CompareTo(other?.DisplayName);

    public override int CompareTo(Entity<TId>? other) => other is not IStadium entity ? -1 : CompareTo(entity);

    public bool IsSimilar(IStadium? obj) => DisplayName.IsSimilar(obj?.DisplayName);

    public bool IsSimilar(object? obj) => obj is IStadium person && IsSimilar(person);
}
