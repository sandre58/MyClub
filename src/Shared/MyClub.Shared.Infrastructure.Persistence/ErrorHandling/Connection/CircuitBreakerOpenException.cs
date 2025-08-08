// -----------------------------------------------------------------------
// <copyright file="CircuitBreakerOpenException.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace MyClub.Shared.Infrastructure.Persistence.ErrorHandling.Connection;

/// <summary>
/// Exception thrown when the circuit breaker is open and database operations are blocked.
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class.
    /// </summary>
    public CircuitBreakerOpenException()
        : base("Circuit breaker is open") =>
        Reason = "Unknown";

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class.
    /// </summary>
    /// <param name="reason">The reason why the circuit breaker is open.</param>
    public CircuitBreakerOpenException(string reason)
        : base($"Circuit breaker is open: {reason}") =>
        Reason = reason;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class.
    /// </summary>
    /// <param name="reason">The reason why the circuit breaker is open.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public CircuitBreakerOpenException(string reason, Exception innerException)
        : base($"Circuit breaker is open: {reason}", innerException) =>
        Reason = reason;

    /// <summary>
    /// Gets the reason why the circuit breaker is open.
    /// </summary>
    public string Reason { get; }
}
