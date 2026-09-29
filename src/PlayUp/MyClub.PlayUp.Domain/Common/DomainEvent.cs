// -----------------------------------------------------------------------
// <copyright file="DomainEvent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Base type for domain events (V1: in-memory observation for tests/audit — not runtime integration).
/// Callers must supply <see cref="OccurredOn"/> via <see cref="IClock"/> or an explicit timestamp — never <c>DateTime.UtcNow</c>.
/// </summary>
/// <remarks>
/// Not persisted (Décision 9.1). Not dispatched in Application V1 (Décision D-06).
/// </remarks>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> class.
    /// </summary>
    /// <param name="occurredOn">The UTC timestamp when the event occurred.</param>
    protected DomainEvent(DateTimeOffset occurredOn) => OccurredOn = occurredOn;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> class using the given clock.
    /// </summary>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    protected DomainEvent(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        OccurredOn = clock.UtcNow;
    }

    /// <inheritdoc />
    public DateTimeOffset OccurredOn { get; }
}
