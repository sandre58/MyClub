// -----------------------------------------------------------------------
// <copyright file="Entity.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using MyClub.Shared.Kernel.Events;

namespace MyClub.Shared.Kernel.Primitives;

/// <summary>
/// Base class for all domain entities in the system.
/// Provides identity, domain event handling, and equality comparison based on strongly-typed IDs.
/// This is the foundation class for implementing Domain-Driven Design entities.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier for the entity, which must inherit from EntityId&lt;TId&gt;.</typeparam>
[DebuggerDisplay("{Id} | {DebuggerDisplayValue}")]
public abstract class Entity<TId>(TId id) : IEntity<TId>, IHasDomainEvents, IEquatable<Entity<TId>>
    where TId : EntityId<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    [UsedImplicitly(Reason = "Used by EF Core.")]
    protected Entity()
        : this(null!) { }

    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    public TId Id { get; } = id;

    /// <summary>
    /// Gets the collection of domain events that have been raised by this entity.
    /// Domain events represent business-significant occurrences within the entity's lifecycle.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Used by DebuggerDisplay attribute")]
    private string DebuggerDisplayValue => ToString();

    /// <summary>
    /// Adds a domain event to this entity's event collection.
    /// Domain events will be dispatched when the unit of work is committed.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Removes a specific domain event from this entity's event collection.
    /// </summary>
    /// <param name="domainEvent">The domain event to remove.</param>
    protected void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);

    /// <summary>
    /// Clears all domain events from this entity.
    /// Typically called after domain events have been dispatched.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// Equality is based on the entity's unique identifier.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>true if the specified object is equal to the current entity; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is Entity<TId> other && Equals(other));

    /// <summary>
    /// Determines whether the specified entity is equal to the current entity.
    /// Equality is based on the entity's unique identifier.
    /// </summary>
    /// <param name="other">The entity to compare with the current entity.</param>
    /// <returns>true if the specified entity is equal to the current entity; otherwise, false.</returns>
    public bool Equals(Entity<TId>? other) =>
        ReferenceEquals(this, other) || (other is not null && Id.Equals(other.Id));

    /// <summary>
    /// Returns the hash code for this entity based on its unique identifier.
    /// </summary>
    /// <returns>A hash code for the current entity.</returns>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Returns a string representation of the entity, including its type name and identifier.
    /// </summary>
    /// <returns>A string that represents the current entity.</returns>
    public override string ToString() => $"{GetType().Name} [{Id}]";
}
