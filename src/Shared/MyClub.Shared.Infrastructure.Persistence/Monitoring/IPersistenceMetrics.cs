// -----------------------------------------------------------------------
// <copyright file="IPersistenceMetrics.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace MyClub.Shared.Infrastructure.Persistence.Monitoring;

/// <summary>
/// Defines the contract for persistence infrastructure metrics collection.
/// Provides methods to record and track performance metrics, errors, and system health indicators
/// for database operations, circuit breaker events, and retry policy executions.
/// </summary>
/// <remarks>
/// This interface enables integration with various monitoring systems (e.g., Prometheus, Application Insights)
/// while keeping the persistence layer independent of specific monitoring implementations.
/// Metrics help track system health, identify performance bottlenecks, and monitor error patterns.
/// </remarks>
public interface IPersistenceMetrics
{
    /// <summary>
    /// Records a database connection failure event.
    /// Used to track connection stability and identify infrastructure issues.
    /// </summary>
    /// <param name="reason">The reason for the connection failure.</param>
    /// <param name="duration">The time taken before the failure occurred.</param>
    void RecordConnectionFailure(string reason, TimeSpan? duration = null);

    /// <summary>
    /// Records a successful database connection event.
    /// Used to track connection success rate and response times.
    /// </summary>
    /// <param name="duration">The time taken to establish the connection.</param>
    void RecordConnectionSuccess(TimeSpan duration);

    /// <summary>
    /// Records a retry attempt for a database operation.
    /// Helps monitor retry policy effectiveness and operation resilience.
    /// </summary>
    /// <param name="operationName">The name of the operation being retried.</param>
    /// <param name="attemptNumber">The current attempt number (1-based).</param>
    /// <param name="delayBeforeRetry">The delay before this retry attempt.</param>
    void RecordRetryAttempt(string operationName, int attemptNumber, TimeSpan delayBeforeRetry);

    /// <summary>
    /// Records a circuit breaker state change.
    /// Critical for monitoring system stability and automatic recovery mechanisms.
    /// </summary>
    /// <param name="previousState">The previous state of the circuit breaker.</param>
    /// <param name="newState">The new state of the circuit breaker.</param>
    /// <param name="reason">The reason for the state change.</param>
    void RecordCircuitBreakerStateChange(string previousState, string newState, string reason);

    /// <summary>
    /// Records the execution time of a database operation.
    /// Used for performance monitoring and identifying slow operations.
    /// </summary>
    /// <param name="operationName">The name of the database operation.</param>
    /// <param name="duration">The execution duration.</param>
    /// <param name="success">Whether the operation was successful.</param>
    void RecordOperationDuration(string operationName, TimeSpan duration, bool success);

    /// <summary>
    /// Records a database error that occurred during operation execution.
    /// Helps track error patterns and system reliability.
    /// </summary>
    /// <param name="operationName">The name of the operation that failed.</param>
    /// <param name="errorType">The type or category of the error.</param>
    /// <param name="isTransient">Whether the error is considered transient and retryable.</param>
    void RecordDatabaseError(string operationName, string errorType, bool isTransient);

    /// <summary>
    /// Records health check results for monitoring system health.
    /// Used to track overall system availability and performance trends.
    /// </summary>
    /// <param name="isHealthy">Whether the health check passed.</param>
    /// <param name="responseTime">The health check response time.</param>
    /// <param name="checkType">The type of health check performed.</param>
    void RecordHealthCheck(bool isHealthy, TimeSpan responseTime, string checkType = "database");

    /// <summary>
    /// Increments a counter for the number of active database connections.
    /// Helps monitor connection pool usage and detect connection leaks.
    /// </summary>
    void IncrementActiveConnections();

    /// <summary>
    /// Decrements a counter for the number of active database connections.
    /// Helps monitor connection pool usage and detect connection leaks.
    /// </summary>
    void DecrementActiveConnections();
}
