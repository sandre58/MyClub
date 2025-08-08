// -----------------------------------------------------------------------
// <copyright file="IPersistenceErrorHandler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyClub.Shared.Infrastructure.Persistence.ErrorHandling.PersistenceError;

/// <summary>
/// Defines the contract for centralized persistence error handling, providing methods
/// for classifying, handling, and recovering from database-related exceptions.
/// </summary>
public interface IPersistenceErrorHandler
{
    /// <summary>
    /// Determines whether the specified exception represents a transient failure
    /// that may succeed if retried after a brief delay.
    /// </summary>
    /// <param name="exception">The exception to analyze.</param>
    /// <returns>True if the exception is transient and retry is recommended; otherwise, false.</returns>
    bool IsTransientFailure(Exception exception);

    /// <summary>
    /// Handles a database exception and determines the appropriate response strategy.
    /// This method centralizes error classification and recovery logic.
    /// </summary>
    /// <param name="exception">The database exception that occurred.</param>
    /// <param name="operationName">The name or description of the operation that failed.</param>
    /// <param name="cancellationToken">Token to cancel the error handling operation.</param>
    /// <returns>A task representing the error handling operation with recovery recommendations.</returns>
    Task<ErrorHandlingResult> HandleExceptionAsync(Exception exception, string operationName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a database operation with automatic error handling and retry logic.
    /// This method provides a consistent approach to executing database operations with resilience.
    /// </summary>
    /// <typeparam name="T">The type of result returned by the operation.</typeparam>
    /// <param name="operation">The database operation to execute.</param>
    /// <param name="operationName">The name or description of the operation for logging purposes.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the operation result.</returns>
    Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string operationName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a database operation with automatic error handling and retry logic.
    /// This overload is for operations that do not return a value.
    /// </summary>
    /// <param name="operation">The database operation to execute.</param>
    /// <param name="operationName">The name or description of the operation for logging purposes.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the operation completion.</returns>
    Task ExecuteWithErrorHandlingAsync(Func<Task> operation, string operationName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a slow query for performance monitoring and analysis.
    /// </summary>
    /// <param name="commandText">The SQL command text that executed slowly.</param>
    /// <param name="duration">The execution duration of the slow query.</param>
    /// <param name="operationName">The name or description of the operation.</param>
    void LogSlowQuery(string commandText, TimeSpan duration, string operationName);

    /// <summary>
    /// Logs a database error for monitoring and debugging purposes.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="operationName">The name or description of the operation that failed.</param>
    /// <param name="additionalContext">Additional context information about the error.</param>
    void LogDatabaseError(Exception exception, string operationName, string? additionalContext = null);
}

/// <summary>
/// Represents the result of error handling operations, indicating the recommended
/// response strategy and any additional context for recovery or escalation.
/// </summary>
public class ErrorHandlingResult
{
    /// <summary>
    /// Gets or sets the recommended action to take in response to the error.
    /// </summary>
    public ErrorAction RecommendedAction { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the operation should be retried.
    /// This is typically true for transient failures.
    /// </summary>
    public bool ShouldRetry { get; set; }

    /// <summary>
    /// Gets or sets the suggested delay before retrying the operation.
    /// This value is used for exponential backoff strategies.
    /// </summary>
    public TimeSpan RetryDelay { get; set; }

    /// <summary>
    /// Gets or sets the processed exception with additional context or wrapper information.
    /// This may be the original exception or a more specific exception type.
    /// </summary>
    public Exception ProcessedException { get; set; } = null!;

    /// <summary>
    /// Gets or sets additional context information about the error and recommended actions.
    /// </summary>
    public string? AdditionalContext { get; set; }

    /// <summary>
    /// Creates a result indicating that the operation should be retried after a delay.
    /// </summary>
    /// <param name="delay">The delay before retrying.</param>
    /// <param name="exception">The original exception.</param>
    /// <returns>An error handling result configured for retry.</returns>
    public static ErrorHandlingResult Retry(TimeSpan delay, Exception exception) => new()
    {
        RecommendedAction = ErrorAction.Retry,
        ShouldRetry = true,
        RetryDelay = delay,
        ProcessedException = exception
    };

    /// <summary>
    /// Creates a result indicating that the operation should fail immediately without retry.
    /// </summary>
    /// <param name="exception">The exception to throw.</param>
    /// <param name="context">Additional context about the failure.</param>
    /// <returns>An error handling result configured for immediate failure.</returns>
    public static ErrorHandlingResult Fail(Exception exception, string? context = null) => new()
    {
        RecommendedAction = ErrorAction.Fail,
        ShouldRetry = false,
        ProcessedException = exception,
        AdditionalContext = context
    };

    /// <summary>
    /// Creates a result indicating that the error has been handled and the operation can continue.
    /// </summary>
    /// <param name="context">Additional context about the handled error.</param>
    /// <returns>An error handling result configured for continuation.</returns>
    public static ErrorHandlingResult Continue(string? context = null) => new()
    {
        RecommendedAction = ErrorAction.Continue,
        ShouldRetry = false,
        ProcessedException = new InvalidOperationException("Error was handled"),
        AdditionalContext = context
    };
}

/// <summary>
/// Defines the possible actions that can be taken in response to a database error.
/// </summary>
public enum ErrorAction
{
    /// <summary>
    /// Retry the operation after a delay.
    /// </summary>
    Retry,

    /// <summary>
    /// Fail the operation immediately.
    /// </summary>
    Fail,

    /// <summary>
    /// Continue execution despite the error.
    /// </summary>
    Continue,

    /// <summary>
    /// Escalate the error to a higher level for manual intervention.
    /// </summary>
    Escalate
}
