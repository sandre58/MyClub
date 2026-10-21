// -----------------------------------------------------------------------
// <copyright file="IDomainEvent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Marker for a domain event raised by an aggregate root (observation side-channel, not a dispatch message).
/// </summary>
/// <remarks>
/// Application and Host do not process these events.
/// </remarks>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    DateTimeOffset OccurredOn { get; }
}
