// -----------------------------------------------------------------------
// <copyright file="DomainEvent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace MyClub.Shared.Kernel.Events;

/// <summary>
/// Base record for domain events that provides a default implementation of IDomainEvent.
/// Domain events represent something significant that happened in the business domain.
/// This base class automatically sets the OccurredOn timestamp when the event is created.
/// Use this as a base for your concrete domain event implementations.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Gets the timestamp when this domain event occurred.
    /// This value is automatically set to the current UTC time when the event is created.
    /// </summary>
    public DateTimeOffset OccurredOn { get; } = DateTime.UtcNow;
}
