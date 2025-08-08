// -----------------------------------------------------------------------
// <copyright file="IHasDomainEvents.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace MyClub.Shared.Kernel.Events;

/// <summary>
/// Interface for entities that can raise and manage domain events.
/// Domain events represent significant business occurrences within an aggregate that other parts of the system need to be aware of.
/// This interface provides the contract for accessing and managing the domain events collection.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Gets the collection of domain events that have been raised by this entity.
    /// These events will be dispatched when the unit of work is committed.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all domain events from this entity.
    /// This method is typically called after domain events have been successfully dispatched.
    /// </summary>
    void ClearDomainEvents();
}
