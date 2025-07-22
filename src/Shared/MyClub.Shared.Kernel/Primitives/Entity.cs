// -----------------------------------------------------------------------
// <copyright file="Entity.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;

namespace MyClub.Shared.Kernel.Primitives;

[DebuggerDisplay("{Id} | {DebuggerDisplayValue}")]
public abstract class Entity<TId>(TId id) : IEntity<TId>
    where TId : EntityId<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    // <remarks>Used by EF Core</remarks>
    protected Entity()
        : this(default!) { }

    public TId Id { get; } = id;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private string? DebuggerDisplayValue => ToString();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    protected void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(Id, other.Id);

    public bool Equals(Entity<TId>? other) => ReferenceEquals(this, other) || (other is not null && EqualityComparer<TId>.Default.Equals(Id, other.Id));

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString() => $"{GetType().Name} [{Id}]";

    #region IComparable

    public virtual int CompareTo(Entity<TId>? other) => other is null ? 1 : Id.CompareTo(other.Id);

    public int CompareTo(object? obj) => obj is Entity<TId> other ? CompareTo(other) : 1;

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);

    public static bool operator >(Entity<TId> left, Entity<TId> right) => left.CompareTo(right) > 0;

    public static bool operator <(Entity<TId> left, Entity<TId> right) => left.CompareTo(right) < 0;

    public static bool operator >=(Entity<TId> left, Entity<TId> right) => left.CompareTo(right) >= 0;

    public static bool operator <=(Entity<TId> left, Entity<TId> right) => left.CompareTo(right) <= 0;

    #endregion
}
