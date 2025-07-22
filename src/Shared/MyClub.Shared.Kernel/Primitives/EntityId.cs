// -----------------------------------------------------------------------
// <copyright file="EntityId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace MyClub.Shared.Kernel.Primitives;

public static class EntityId
{
    public static TEntityId From<TEntityId>(Guid id)
        where TEntityId : EntityId<TEntityId> => (TEntityId)Activator.CreateInstance(typeof(TEntityId), id)!;

    public static TEntityId New<TEntityId>()
        where TEntityId : EntityId<TEntityId> => From<TEntityId>(Guid.NewGuid());
}

public abstract record EntityId<TSelf> : IEquatable<TSelf>, IComparable<TSelf>
    where TSelf : EntityId<TSelf>
{
    public Guid Value { get; }

    protected EntityId(Guid value) => Value = value;

    public static TSelf From(Guid id) => EntityId.From<TSelf>(id);

    public static TSelf New() => EntityId.New<TSelf>();

    public static readonly TSelf Empty = (TSelf)Activator.CreateInstance(typeof(TSelf), Guid.Empty)!;

    public bool Equals(TSelf? other) => Value.Equals(other?.Value);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();

    public int CompareTo(TSelf? other) => Value.CompareTo(other?.Value);

    public static bool operator <(EntityId<TSelf> left, EntityId<TSelf> right) => left is not null && left.CompareTo((TSelf)right) < 0;

    public static bool operator <=(EntityId<TSelf> left, EntityId<TSelf> right) => left is not null && left.CompareTo((TSelf)right) <= 0;

    public static bool operator >(EntityId<TSelf> left, EntityId<TSelf> right) => left is not null && left.CompareTo((TSelf)right) > 0;

    public static bool operator >=(EntityId<TSelf> left, EntityId<TSelf> right) => left is not null && left.CompareTo((TSelf)right) >= 0;

    public static implicit operator Guid(EntityId<TSelf> entityId) => ToGuid(entityId);

    public static implicit operator EntityId<TSelf>(Guid guid) => ToEntityId(guid);

    public static EntityId<TSelf> ToEntityId(Guid guid) => From(guid);

    public static Guid ToGuid(EntityId<TSelf> entityId) => entityId.Value;
}
