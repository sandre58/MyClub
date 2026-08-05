// -----------------------------------------------------------------------
// <copyright file="AggregateRoot.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Base type for aggregate roots. Collects domain events raised during a mutation until cleared by the application pipeline.
/// </summary>
/// <typeparam name="TId">The typed identifier of the aggregate.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="AggregateRoot{TId}"/> class.
/// </remarks>
/// <param name="id">The aggregate identity.</param>
[DebuggerDisplay("{GetType().Name} {Id}")]
public abstract class AggregateRoot<TId>(TId id) : Entity<TId>(id)
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the uncommitted domain events in raise order (FIFO).
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Clears uncommitted domain events after they have been dispatched by the application pipeline.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Raises a domain event that will be collected until <see cref="ClearDomainEvents"/> is called.
    /// </summary>
    /// <param name="domainEvent">The event to raise.</param>
    protected void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
