// -----------------------------------------------------------------------
// <copyright file="ErrorHandlingResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace MyClub.Shared.Application.Abstractions.ErrorHandling;

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
