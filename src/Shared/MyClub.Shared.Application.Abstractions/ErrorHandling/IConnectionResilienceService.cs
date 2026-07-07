// -----------------------------------------------------------------------
// <copyright file="IConnectionResilienceService.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyClub.Shared.Application.Abstractions.ErrorHandling;

/// <summary>
/// Defines the contract for connection resilience services that monitor and manage
/// database connection health, implementing circuit breaker patterns and connection pooling optimization.
/// </summary>
public interface IConnectionResilienceService
{
    /// <summary>
    /// Gets a value indicating whether the database connection is currently healthy and available.
    /// </summary>
    bool IsConnectionHealthy { get; }

    /// <summary>
    /// Gets the current state of the circuit breaker for database connections.
    /// </summary>
    CircuitBreakerState CircuitBreakerState { get; }

    /// <summary>
    /// Executes a database operation through the circuit breaker pattern,
    /// preventing cascading failures when the database becomes unavailable.
    /// </summary>
    /// <typeparam name="T">The type of result returned by the operation.</typeparam>
    /// <param name="operation">The database operation to execute.</param>
    /// <param name="operationName">The name of the operation for monitoring purposes.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the operation result.</returns>
    Task<T> ExecuteWithCircuitBreakerAsync<T>(Func<Task<T>> operation, string operationName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a database operation through the circuit breaker pattern.
    /// This overload is for operations that do not return a value.
    /// </summary>
    /// <param name="operation">The database operation to execute.</param>
    /// <param name="operationName">The name of the operation for monitoring purposes.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the operation completion.</returns>
    Task ExecuteWithCircuitBreakerAsync(Func<Task> operation, string operationName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on the database connection to verify availability.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the health check.</param>
    /// <returns>A task representing the health check result.</returns>
    Task<ConnectionHealthResult> CheckConnectionHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually opens the circuit breaker, preventing further database operations until recovery.
    /// This can be used for planned maintenance or when external monitoring detects issues.
    /// </summary>
    /// <param name="reason">The reason for opening the circuit breaker.</param>
    void OpenCircuitBreaker(string reason);

    /// <summary>
    /// Manually closes the circuit breaker, allowing database operations to resume.
    /// This should only be used after verifying that the database is healthy.
    /// </summary>
    void CloseCircuitBreaker();

    /// <summary>
    /// Resets connection failure statistics and circuit breaker state.
    /// </summary>
    void ResetConnectionState();
}
