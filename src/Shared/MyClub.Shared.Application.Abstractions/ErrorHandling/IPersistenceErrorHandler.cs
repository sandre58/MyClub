// -----------------------------------------------------------------------
// <copyright file="IPersistenceErrorHandler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyClub.Shared.Application.Abstractions.ErrorHandling;

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
