// -----------------------------------------------------------------------
// <copyright file="Person.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Domain.Extensions;
using MyClub.Shared.Kernel.Primitives;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Shared.Domain.Persons;

public abstract class Person<TId> : AuditableEntity<TId>, IPerson
    where TId : EntityId<TId>
{
    // <remarks>Used by EF Core</remarks>
    protected Person()
        : base() { }

    protected Person(TId id, string firstName, string lastName)
        : base(id) => Rename(firstName, lastName);

    public string LastName { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;

    public Country? Country { get; set; }

    public byte[]? Photo { get; set; }

    public GenderType Gender { get; set; } = GenderType.Male;

    public string? LicenseNumber { get; set; }

    public string? Email { get; set; }

    public void Rename(string firstName, string lastName)
    {
        FirstName = firstName.IsRequiredOrThrow();
        LastName = lastName.IsRequiredOrThrow();
    }

    public override string ToString() => this.GetInverseName();

    public int CompareTo(IPerson? other) => this.GetInverseName() != other?.GetInverseName() ? string.Compare(this.GetInverseName(), other?.GetInverseName(), StringComparison.OrdinalIgnoreCase) : base.CompareTo(other);

    public override int CompareTo(Entity<TId>? other) => other is not IPerson entity ? -1 : CompareTo(entity);

    public bool IsSimilar(IPerson? obj) => this.GetInverseName().Equals(obj?.GetInverseName(), StringComparison.OrdinalIgnoreCase);

    public bool IsSimilar(object? obj) => obj is IPerson person && IsSimilar(person);
}
