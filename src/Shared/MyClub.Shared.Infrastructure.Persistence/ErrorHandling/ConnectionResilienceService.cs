// -----------------------------------------------------------------------
// <copyright file="ConnectionResilienceService.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyClub.Shared.Application.Abstractions.ErrorHandling;
using MyClub.Shared.Application.Abstractions.Monitoring;

namespace MyClub.Shared.Infrastructure.Persistence.ErrorHandling.Connection;

/// <summary>
/// Implements connection resilience patterns including circuit breaker functionality
/// to prevent cascading failures and provide graceful degradation for database operations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConnectionResilienceService"/> class.
/// </remarks>
/// <param name="options">Configuration options for connection resilience behavior.</param>
/// <param name="logger">Logger for monitoring and diagnostics.</param>
/// <param name="metrics">Metrics recorder for persistence monitoring.</param>
public sealed partial class ConnectionResilienceService(
    PersistenceErrorHandlingOptions options,
    ILogger<ConnectionResilienceService> logger,
    IPersistenceMetrics metrics) : IConnectionResilienceService
{
    private readonly IPersistenceMetrics _metrics = metrics;
    private readonly Lock _lockObject = new();

    private int _consecutiveFailures;
    private DateTime _circuitOpenTime = DateTime.MinValue;
    private string _circuitBreakerReason = string.Empty;

    #region LoggerMessage Definitions

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Database health check completed - Healthy: {IsHealthy}, Response time: {ResponseTime}ms")]
    private static partial void LogHealthCheckCompleted(ILogger logger, bool isHealthy, double responseTime);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Database health check failed after {ResponseTime}ms")]
    private static partial void LogHealthCheckFailed(ILogger logger, Exception exception, double responseTime);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Circuit breaker manually opened: {Reason}")]
    private static partial void LogCircuitBreakerManuallyOpened(ILogger logger, string reason);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Circuit breaker manually closed - operations can resume")]
    private static partial void LogCircuitBreakerManuallyClosed(ILogger logger);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Connection state reset - circuit breaker closed, failure count cleared")]
    private static partial void LogConnectionStateReset(ILogger logger);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "Circuit breaker transitioning to half-open state for recovery attempt")]
    private static partial void LogCircuitBreakerTransitioningToHalfOpen(ILogger logger);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "Circuit breaker closed after successful recovery attempt")]
    private static partial void LogCircuitBreakerClosedAfterRecovery(ILogger logger);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Warning,
        Message = "Database operation '{OperationName}' failed (consecutive failures: {ConsecutiveFailures}): {ExceptionMessage}")]
    private static partial void LogDatabaseOperationFailed(ILogger logger, string operationName, int consecutiveFailures, string exceptionMessage);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Error,
        Message = "Circuit breaker opened due to {ConsecutiveFailures} consecutive failures. Last error: {ExceptionMessage}")]
    private static partial void LogCircuitBreakerOpened(ILogger logger, int consecutiveFailures, string exceptionMessage);

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Warning,
        Message = "Circuit breaker returned to open state after failed recovery attempt")]
    private static partial void LogCircuitBreakerReturnedToOpen(ILogger logger);

    #endregion

    /// <summary>
    /// Gets a value indicating whether the database connection is currently healthy and available.
    /// </summary>
    public bool IsConnectionHealthy => CircuitBreakerState == CircuitBreakerState.Closed;

    /// <summary>
    /// Gets the current state of the circuit breaker for database connections.
    /// </summary>
    public CircuitBreakerState CircuitBreakerState { get; private set; } = CircuitBreakerState.Closed;

    /// <summary>
    /// Executes a database operation through the circuit breaker pattern.
    /// </summary>
    /// <typeparam name="T">The type of result returned by the operation.</typeparam>
    /// <param name="operation">The database operation to execute.</param>
    /// <param name="operationName">The name of the operation for monitoring purposes.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the operation result.</returns>
    public async Task<T> ExecuteWithCircuitBreakerAsync<T>(Func<Task<T>> operation, string operationName, CancellationToken cancellationToken = default)
    {
        if (!options.EnableCircuitBreaker)
            return await operation().ConfigureAwait(false);

        // Check if circuit breaker allows execution
        await CheckCircuitBreakerStateAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var result = await operation().ConfigureAwait(false);

            // Operation succeeded, record success
            RecordSuccess();

            return result;
        }
        catch (Exception ex)
        {
            // Operation failed, record failure
            RecordFailure(ex, operationName);
            throw;
        }
    }

    /// <summary>
    /// Executes a database operation through the circuit breaker pattern.
    /// </summary>
    /// <param name="operation">The database operation to execute.</param>
    /// <param name="operationName">The name of the operation for monitoring purposes.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the operation completion.</returns>
    public async Task ExecuteWithCircuitBreakerAsync(Func<Task> operation, string operationName, CancellationToken cancellationToken = default) =>
        await ExecuteWithCircuitBreakerAsync(async () =>
            {
                await operation().ConfigureAwait(false);
                return true;
            },
            operationName,
            cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Performs a health check on the database connection to verify availability.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the health check.</param>
    /// <returns>A task representing the health check result.</returns>
    public async Task<ConnectionHealthResult> CheckConnectionHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _metrics.IncrementActiveConnections();
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            var result = new ConnectionHealthResult
            {
                IsHealthy = CircuitBreakerState != CircuitBreakerState.Open,
                ResponseTime = stopwatch.Elapsed,
                DiagnosticInfo = $"Circuit breaker state: {CircuitBreakerState}, Consecutive failures: {_consecutiveFailures}"
            };
            LogHealthCheckCompleted(logger, result.IsHealthy, result.ResponseTime.TotalMilliseconds);
            _metrics.RecordHealthCheck(result.IsHealthy, result.ResponseTime, "database");
            if (result.IsHealthy)
                _metrics.RecordConnectionSuccess(result.ResponseTime);
            else
                _metrics.RecordConnectionFailure("Health check failed", result.ResponseTime);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var result = new ConnectionHealthResult
            {
                IsHealthy = false,
                ResponseTime = stopwatch.Elapsed,
                ErrorMessage = ex.Message,
                DiagnosticInfo = $"Health check failed: {ex.GetType().Name}"
            };
            LogHealthCheckFailed(logger, ex, result.ResponseTime.TotalMilliseconds);
            _metrics.RecordHealthCheck(false, result.ResponseTime, "database");
            _metrics.RecordConnectionFailure(ex.Message, result.ResponseTime);
            throw;
        }
        finally
        {
            _metrics.DecrementActiveConnections();
        }
    }

    /// <summary>
    /// Manually opens the circuit breaker, preventing further database operations until recovery.
    /// </summary>
    /// <param name="reason">The reason for opening the circuit breaker.</param>
    public void OpenCircuitBreaker(string reason)
    {
        lock (_lockObject)
        {
            var previousState = CircuitBreakerState.ToString();
            CircuitBreakerState = CircuitBreakerState.Open;
            _circuitOpenTime = DateTime.UtcNow;
            _circuitBreakerReason = reason;
            LogCircuitBreakerManuallyOpened(logger, reason);
            _metrics.RecordCircuitBreakerStateChange(previousState, CircuitBreakerState.ToString(), reason);
        }
    }

    /// <summary>
    /// Manually closes the circuit breaker, allowing database operations to resume.
    /// </summary>
    public void CloseCircuitBreaker()
    {
        lock (_lockObject)
        {
            var previousState = CircuitBreakerState.ToString();
            CircuitBreakerState = CircuitBreakerState.Closed;
            _consecutiveFailures = 0;
            _circuitBreakerReason = string.Empty;
            LogCircuitBreakerManuallyClosed(logger);
            _metrics.RecordCircuitBreakerStateChange(previousState, CircuitBreakerState.ToString(), "Manual close");
        }
    }

    /// <summary>
    /// Resets connection failure statistics and circuit breaker state.
    /// </summary>
    public void ResetConnectionState()
    {
        lock (_lockObject)
        {
            var previousState = CircuitBreakerState.ToString();
            CircuitBreakerState = CircuitBreakerState.Closed;
            _consecutiveFailures = 0;
            _circuitOpenTime = DateTime.MinValue;
            _circuitBreakerReason = string.Empty;
            LogConnectionStateReset(logger);
            _metrics.RecordCircuitBreakerStateChange(previousState, CircuitBreakerState.ToString(), "Reset");
        }
    }

    #region Private Helper Methods

    private Task CheckCircuitBreakerStateAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        lock (_lockObject)
        {
            switch (CircuitBreakerState)
            {
                case CircuitBreakerState.Closed:
                    // Normal operation, allow request
                    return Task.CompletedTask;

                case CircuitBreakerState.Open:
                    // Check if timeout period has elapsed
                    var timeoutPeriod = TimeSpan.FromSeconds(options.CircuitBreakerTimeoutSeconds);
                    if (DateTime.UtcNow - _circuitOpenTime < timeoutPeriod)
                        throw new CircuitBreakerOpenException(_circuitBreakerReason);

                    // Transition to half-open state
                    CircuitBreakerState = CircuitBreakerState.HalfOpen;

                    // Use LoggerMessage delegate for better performance
                    LogCircuitBreakerTransitioningToHalfOpen(logger);
                    return Task.CompletedTask;

                case CircuitBreakerState.HalfOpen:
                default:
                    return Task.CompletedTask;
            }
        }
    }

    private void RecordSuccess()
    {
        lock (_lockObject)
        {
            if (CircuitBreakerState != CircuitBreakerState.HalfOpen)
            {
                if (CircuitBreakerState == CircuitBreakerState.Closed)
                {
                    // Normal operation, reset failure count
                    _consecutiveFailures = 0;
                }
            }
            else
            {
                // Recovery successful, close the circuit
                CircuitBreakerState = CircuitBreakerState.Closed;
                _consecutiveFailures = 0;
                _circuitBreakerReason = string.Empty;

                // Use LoggerMessage delegate for better performance
                LogCircuitBreakerClosedAfterRecovery(logger);
            }
        }
    }

    private void RecordFailure(Exception exception, string operationName)
    {
        lock (_lockObject)
        {
            _consecutiveFailures++;

            // Use LoggerMessage delegate for better performance
            LogDatabaseOperationFailed(logger, operationName, _consecutiveFailures, exception.Message);

            // Check if we should open the circuit breaker
            if (_consecutiveFailures >= options.CircuitBreakerFailureThreshold)
            {
                if (CircuitBreakerState == CircuitBreakerState.Open) return;
                CircuitBreakerState = CircuitBreakerState.Open;
                _circuitOpenTime = DateTime.UtcNow;
                _circuitBreakerReason = $"Failure threshold exceeded ({_consecutiveFailures} consecutive failures)";

                // Use LoggerMessage delegate for better performance
                LogCircuitBreakerOpened(logger, _consecutiveFailures, exception.Message);
            }
            else if (CircuitBreakerState == CircuitBreakerState.HalfOpen)
            {
                // Half-open test failed, return to open state
                CircuitBreakerState = CircuitBreakerState.Open;
                _circuitOpenTime = DateTime.UtcNow;
                _circuitBreakerReason = "Recovery attempt failed";

                // Use LoggerMessage delegate for better performance
                LogCircuitBreakerReturnedToOpen(logger);
            }
        }
    }

    #endregion
}
