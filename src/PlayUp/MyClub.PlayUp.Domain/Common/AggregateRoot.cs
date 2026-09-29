// -----------------------------------------------------------------------
// <copyright file="AggregateRoot.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Base type for aggregate roots. Collects domain events raised during a mutation in an in-memory FIFO buffer.
/// </summary>
/// <typeparam name="TId">The typed identifier of the aggregate.</typeparam>
/// <remarks>
/// <para>
/// Play'Up V1: domain events are an <strong>observation side-channel</strong> (tests / optional technical audit).
/// They are not dispatched by Application, not persisted, and are not the source of truth for behaviour —
/// aggregate state and invariants are. See Décision D-06.
/// </para>
/// <para>
/// Initializes a new instance of the <see cref="AggregateRoot{TId}"/> class.
/// </para>
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
    /// <remarks>
    /// V1: consumed by tests (and optionally cleared there). Not a runtime Application pipeline API.
    /// </remarks>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Clears the in-memory event buffer (typically after test assertions).
    /// </summary>
    /// <remarks>
    /// V1 does not dispatch these events. Clearing is not a post-handler step of a production pipeline.
    /// </remarks>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Appends a domain event to the in-memory buffer until <see cref="ClearDomainEvents"/> or instance disposal.
    /// </summary>
    /// <param name="domainEvent">The event to raise.</param>
    protected void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
