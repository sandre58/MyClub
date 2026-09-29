// -----------------------------------------------------------------------
// <copyright file="IDomainEvent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Marker for a domain event raised by an aggregate root (V1: observation side-channel, not a dispatch message).
/// </summary>
/// <remarks>
/// Play'Up V1 does not process these events in Application/Host. See Décision D-06.
/// </remarks>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    DateTimeOffset OccurredOn { get; }
}
