// -----------------------------------------------------------------------
// <copyright file="PersistenceErrorHandlingOptions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace MyClub.Shared.Infrastructure.Persistence.ErrorHandling;

/// <summary>
/// Configuration options for persistence layer error handling, providing fine-grained control
/// over exception management, retry policies, and recovery strategies for database operations.
/// </summary>
public class PersistenceErrorHandlingOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts for transient database failures.
    /// Transient failures include connection timeouts, deadlocks, and temporary network issues.
    /// Default is 3 attempts.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay between retry attempts in milliseconds.
    /// The actual delay uses exponential backoff: delay = BaseRetryDelay * (2^attempt).
    /// Default is 1000ms (1 second).
    /// </summary>
    public int BaseRetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum delay between retry attempts in milliseconds.
    /// This caps the exponential backoff to prevent excessively long delays.
    /// Default is 30000ms (30 seconds).
    /// </summary>
    public int MaxRetryDelayMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the command timeout in seconds for database operations.
    /// Commands exceeding this timeout will be cancelled and may trigger retry logic.
    /// Default is 30 seconds.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the connection timeout in seconds for establishing database connections.
    /// Default is 30 seconds.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to enable automatic retry for transient failures.
    /// When disabled, transient failures will be thrown immediately without retry attempts.
    /// Default is true.
    /// </summary>
    public bool EnableRetryOnTransientFailures { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable detailed error logging.
    /// When enabled, full exception details including stack traces are logged.
    /// Should be disabled in production for security reasons.
    /// Default is false.
    /// </summary>
    public bool EnableDetailedErrorLogging { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable performance monitoring.
    /// When enabled, slow queries and database operations are logged for analysis.
    /// Default is true.
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets the threshold in milliseconds for slow query detection.
    /// Database commands exceeding this duration will be logged as slow queries.
    /// Default is 5000ms (5 seconds).
    /// </summary>
    public int SlowQueryThresholdMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets a value indicating whether to enable circuit breaker pattern
    /// for database operations to prevent cascading failures.
    /// Default is true.
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;

    /// <summary>
    /// Gets or sets the failure threshold for the circuit breaker.
    /// The circuit opens after this many consecutive failures.
    /// Default is 5 failures.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the time in seconds the circuit breaker stays open before attempting recovery.
    /// Default is 60 seconds.
    /// </summary>
    public int CircuitBreakerTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets a custom action to execute when a database error occurs.
    /// This allows for custom error handling logic such as notifications or metrics collection.
    /// </summary>
    public Action<Exception, string>? OnDatabaseError { get; set; }

    /// <summary>
    /// Gets or sets a custom action to execute when a slow query is detected.
    /// This allows for custom monitoring and alerting for performance issues.
    /// </summary>
    public Action<string, TimeSpan>? OnSlowQuery { get; set; }

    /// <summary>
    /// Gets or sets a custom action to execute when retry attempts are made.
    /// This allows for custom logging and monitoring of retry behavior.
    /// </summary>
    public Action<Exception, int, TimeSpan>? OnRetryAttempt { get; set; }
}
