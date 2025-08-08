// -----------------------------------------------------------------------
// <copyright file="NullPersistenceMetrics.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace MyClub.Shared.Infrastructure.Persistence.Monitoring;

/// <summary>
/// Null object implementation of IPersistenceMetrics that performs no operations.
/// Used as default implementation when no specific metrics provider is configured.
/// Follows the Null Object pattern to avoid conditional checks throughout the codebase.
/// </summary>
/// <remarks>
/// This implementation allows the persistence infrastructure to operate without
/// requiring a specific metrics provider to be configured. Applications can
/// replace this with concrete implementations (e.g., Prometheus, Application Insights)
/// through dependency injection without changing the persistence layer code.
/// </remarks>
public sealed class NullPersistenceMetrics : IPersistenceMetrics
{
    /// <summary>
    /// Gets the singleton instance of the null metrics implementation.
    /// </summary>
    public static readonly IPersistenceMetrics Instance = new NullPersistenceMetrics();

    private NullPersistenceMetrics() { }

    /// <inheritdoc />
    public void RecordConnectionFailure(string reason, TimeSpan? duration = null) { }

    /// <inheritdoc />
    public void RecordConnectionSuccess(TimeSpan duration) { }

    /// <inheritdoc />
    public void RecordRetryAttempt(string operationName, int attemptNumber, TimeSpan delayBeforeRetry) { }

    /// <inheritdoc />
    public void RecordCircuitBreakerStateChange(string previousState, string newState, string reason) { }

    /// <inheritdoc />
    public void RecordOperationDuration(string operationName, TimeSpan duration, bool success) { }

    /// <inheritdoc />
    public void RecordDatabaseError(string operationName, string errorType, bool isTransient) { }

    /// <inheritdoc />
    public void RecordHealthCheck(bool isHealthy, TimeSpan responseTime, string checkType = "database") { }

    /// <inheritdoc />
    public void IncrementActiveConnections() { }

    /// <inheritdoc />
    public void DecrementActiveConnections() { }
}
