// -----------------------------------------------------------------------
// <copyright file="CircuitBreakerState.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Shared.Infrastructure.Persistence.ErrorHandling.Connection;

/// <summary>
/// Represents the possible states of a circuit breaker for database connections.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// The circuit is closed and operations are flowing normally.
    /// </summary>
    Closed,

    /// <summary>
    /// The circuit is open and operations are being blocked due to failures.
    /// </summary>
    Open,

    /// <summary>
    /// The circuit is in half-open state, allowing limited operations to test recovery.
    /// </summary>
    HalfOpen
}
