// -----------------------------------------------------------------------
// <copyright file="PersistenceErrorHandler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyClub.Shared.Infrastructure.Persistence.ErrorHandling.RetryPolicy;

namespace MyClub.Shared.Infrastructure.Persistence.ErrorHandling.PersistenceError;

/// <summary>
/// Centralized persistence error handler that provides consistent error handling,
/// retry logic, and recovery strategies for database operations in the Scorer module.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PersistenceErrorHandler"/> class.
/// </remarks>
/// <param name="options">Configuration options for error handling behavior.</param>
/// <param name="logger">Logger for error tracking and diagnostics.</param>
/// <param name="retryPolicy">Retry policy for transient failure handling.</param>
public sealed partial class PersistenceErrorHandler(
    PersistenceErrorHandlingOptions options,
    ILogger<PersistenceErrorHandler> logger,
    IDatabaseRetryPolicy retryPolicy) : IPersistenceErrorHandler
{
    #region LoggerMessage Definitions

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Retrying operation '{OperationName}' (attempt {AttemptCount}/{MaxAttempts}) after {Delay}ms due to: {Exception}")]
    private static partial void LogRetryAttempt(ILogger logger, string operationName, int attemptCount, int maxAttempts, double delay, string exception);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Slow query detected in operation '{OperationName}' - Duration: {Duration}ms{NewLine}{CommandText}")]
    private static partial void LogSlowQueryDetected(ILogger logger, string operationName, double duration, string newLine, string commandText);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Database error in operation '{OperationName}'{Context}: {ExceptionMessage}")]
    private static partial void LogDatabaseErrorWarning(ILogger logger, string operationName, string context, string exceptionMessage);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "Database error in operation '{OperationName}'{Context}: {ExceptionMessage}")]
    private static partial void LogDatabaseErrorError(ILogger logger, string operationName, string context, string exceptionMessage);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "Database error in operation '{OperationName}'{Context}: {ExceptionDetails}")]
    private static partial void LogDatabaseErrorDetailedWarning(ILogger logger, Exception exception, string operationName, string context, Exception exceptionDetails);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Error,
        Message = "Database error in operation '{OperationName}'{Context}: {ExceptionDetails}")]
    private static partial void LogDatabaseErrorDetailedError(ILogger logger, Exception exception, string operationName, string context, Exception exceptionDetails);

    #endregion

    /// <summary>
    /// Determines whether the specified exception represents a transient failure
    /// that may succeed if retried after a brief delay.
    /// </summary>
    /// <param name="exception">The exception to analyze.</param>
    /// <returns>True if the exception is transient and retry is recommended; otherwise, false.</returns>
    public bool IsTransientFailure(Exception exception) =>
        exception switch
        {
            // Entity Framework Core transient exceptions
            DbUpdateException dbUpdateEx when IsTransientDbUpdateException(dbUpdateEx) => true,

            // General database connection issues
            DbException dbEx => IsTransientDbException(dbEx),

            // Timeout exceptions
            TimeoutException => true,
            OperationCanceledException => false, // User-initiated cancellation, not transient

            // Network-related exceptions
            System.Net.Sockets.SocketException => true,
            System.Net.NetworkInformation.NetworkInformationException => true,

            _ => false
        };

    /// <summary>
    /// Handles a database exception and determines the appropriate response strategy.
    /// </summary>
    /// <param name="exception">The database exception that occurred.</param>
    /// <param name="operationName">The name or description of the operation that failed.</param>
    /// <param name="cancellationToken">Token to cancel the error handling operation.</param>
    /// <returns>A task representing the error handling operation with recovery recommendations.</returns>
    public async Task<ErrorHandlingResult> HandleExceptionAsync(Exception exception, string operationName, CancellationToken cancellationToken = default)
    {
        // Log the error for monitoring and debugging
        LogDatabaseError(exception, operationName);

        // Check if this is a transient failure that should be retried
        if (!options.EnableRetryOnTransientFailures || !IsTransientFailure(exception))
            return ErrorHandlingResult.Fail(exception, "Non-transient failure or retry disabled");

        // Calculate retry delay using exponential backoff
        var retryDelay = await retryPolicy.CalculateDelayAsync(exception, operationName, cancellationToken).ConfigureAwait(false);

        // Execute custom error handling logic if configured
        options.OnDatabaseError?.Invoke(exception, operationName);

        return ErrorHandlingResult.Retry(retryDelay, exception);
    }

    /// <summary>
    /// Executes a database operation with automatic error handling and retry logic.
    /// </summary>
    /// <typeparam name="T">The type of result returned by the operation.</typeparam>
    /// <param name="operation">The database operation to execute.</param>
    /// <param name="operationName">The name or description of the operation for logging purposes.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the operation result.</returns>
    public async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string operationName, CancellationToken cancellationToken = default)
    {
        var attemptCount = 0;
        var maxAttempts = options.MaxRetryAttempts + 1; // +1 for initial attempt

        while (attemptCount < maxAttempts)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (Exception ex) when (attemptCount < maxAttempts - 1)
            {
                attemptCount++;

                var errorResult = await HandleExceptionAsync(ex, operationName, cancellationToken).ConfigureAwait(false);

                if (!errorResult.ShouldRetry)
                    throw errorResult.ProcessedException;

                // Log retry attempt using LoggerMessage delegate
                LogRetryAttempt(logger, operationName, attemptCount, maxAttempts - 1, errorResult.RetryDelay.TotalMilliseconds, ex.Message);

                // Execute custom retry logic if configured
                options.OnRetryAttempt?.Invoke(ex, attemptCount, errorResult.RetryDelay);

                // Wait before retrying
                await Task.Delay(errorResult.RetryDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        // This should never be reached due to the exception handling logic above
        throw new InvalidOperationException("Maximum retry attempts exceeded without successful execution");
    }

    /// <summary>
    /// Executes a database operation with automatic error handling and retry logic.
    /// </summary>
    /// <param name="operation">The database operation to execute.</param>
    /// <param name="operationName">The name or description of the operation for logging purposes.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the operation completion.</returns>
    public async Task ExecuteWithErrorHandlingAsync(Func<Task> operation, string operationName, CancellationToken cancellationToken = default) =>
        await ExecuteWithErrorHandlingAsync(async () =>
            {
                await operation().ConfigureAwait(false);
                return true; // Return dummy value for generic method compatibility
            },
            operationName,
            cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Logs a slow query for performance monitoring and analysis.
    /// </summary>
    /// <param name="commandText">The SQL command text that executed slowly.</param>
    /// <param name="duration">The execution duration of the slow query.</param>
    /// <param name="operationName">The name or description of the operation.</param>
    public void LogSlowQuery(string commandText, TimeSpan duration, string operationName)
    {
        if (!options.EnablePerformanceMonitoring)
            return;

        // Use LoggerMessage delegate for better performance
        LogSlowQueryDetected(logger, operationName, duration.TotalMilliseconds, Environment.NewLine, commandText);

        // Execute custom slow query handling if configured
        options.OnSlowQuery?.Invoke(commandText, duration);
    }

    /// <summary>
    /// Logs a database error for monitoring and debugging purposes.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="operationName">The name or description of the operation that failed.</param>
    /// <param name="additionalContext">Additional context information about the error.</param>
    public void LogDatabaseError(Exception exception, string operationName, string? additionalContext = null)
    {
        var logLevel = IsTransientFailure(exception) ? LogLevel.Warning : LogLevel.Error;
        var context = string.IsNullOrEmpty(additionalContext) ? string.Empty : $" ({additionalContext})";

        if (options.EnableDetailedErrorLogging)
        {
            // Use LoggerMessage delegates with exception parameter
            if (logLevel == LogLevel.Warning)
                LogDatabaseErrorDetailedWarning(logger, exception, operationName, context, exception);
            else
                LogDatabaseErrorDetailedError(logger, exception, operationName, context, exception);
        }
        else
        {
            // Use LoggerMessage delegates without exception parameter
            if (logLevel == LogLevel.Warning)
                LogDatabaseErrorWarning(logger, operationName, context, exception.Message);
            else
                LogDatabaseErrorError(logger, operationName, context, exception.Message);
        }
    }

    #region Private Helper Methods

    private static bool IsTransientDbUpdateException(DbUpdateException exception) =>
        exception.InnerException is DbException dbEx && IsTransientDbException(dbEx);

    private static bool IsTransientDbException(DbException exception)
    {
        // General database exception patterns that might be transient
        var message = exception.Message.ToUpperInvariant();
        return message.Contains("timeout", StringComparison.InvariantCultureIgnoreCase) ||
               message.Contains("network", StringComparison.InvariantCultureIgnoreCase) ||
               message.Contains("connection", StringComparison.InvariantCultureIgnoreCase) ||
               message.Contains("deadlock", StringComparison.InvariantCultureIgnoreCase) ||
               message.Contains("lock", StringComparison.InvariantCultureIgnoreCase) ||
               message.Contains("busy", StringComparison.InvariantCultureIgnoreCase);
    }

    #endregion
}
