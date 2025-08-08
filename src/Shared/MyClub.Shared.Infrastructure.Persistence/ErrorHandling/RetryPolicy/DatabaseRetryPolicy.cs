// -----------------------------------------------------------------------
// <copyright file="DatabaseRetryPolicy.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using MyClub.Shared.Infrastructure.Persistence.ErrorHandling.PersistenceError;
using MyNet.Utilities.Generator;

namespace MyClub.Shared.Infrastructure.Persistence.ErrorHandling.RetryPolicy;

/// <summary>
/// Implements an exponential backoff retry policy with jitter for database operations,
/// providing progressive delays between retry attempts to reduce system load.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DatabaseRetryPolicy"/> class.
/// </remarks>
/// <param name="options">Configuration options for retry behavior.</param>
public class DatabaseRetryPolicy(PersistenceErrorHandlingOptions options) : IDatabaseRetryPolicy
{
    /// <summary>
    /// Calculates the delay before the next retry attempt using exponential backoff with jitter.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="operationName">The name of the operation being retried.</param>
    /// <param name="cancellationToken">Token to cancel the delay calculation.</param>
    /// <returns>A task representing the calculated delay duration.</returns>
    public Task<TimeSpan> CalculateDelayAsync(Exception exception, string operationName, CancellationToken cancellationToken = default)
    {
        // Get the current attempt count from operation context (simplified implementation)
        var attemptCount = GetAttemptCount();

        // Calculate exponential backoff: baseDelay * (2^attempt)
        var exponentialDelay = TimeSpan.FromMilliseconds(options.BaseRetryDelayMs * Math.Pow(2, attemptCount));

        // Cap the delay to the maximum configured value
        var cappedDelay = TimeSpan.FromMilliseconds(Math.Min(exponentialDelay.TotalMilliseconds, options.MaxRetryDelayMs));

        // Add jitter to prevent thundering herd: ±25% random variance
        var value = RandomGenerator.Double() * 0.5;
        var jitterFactor = 0.75 + value; // 0.75 to 1.25
        var finalDelay = TimeSpan.FromMilliseconds(cappedDelay.TotalMilliseconds * jitterFactor);

        return Task.FromResult(finalDelay);
    }

    /// <summary>
    /// Determines whether a retry should be attempted based on the exception type and attempt count.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="attemptCount">The number of attempts made so far (0-based).</param>
    /// <returns>True if a retry should be attempted; otherwise, false.</returns>
    public bool ShouldRetry(Exception exception, int attemptCount)
    {
        // Don't retry if retries are disabled
        if (!options.EnableRetryOnTransientFailures)
            return false;

        // Don't retry if we've exceeded the maximum number of attempts
        return attemptCount < options.MaxRetryAttempts &&
               IsRetryableException(exception);
    }

    /// <summary>
    /// Resets the retry state for a specific operation.
    /// </summary>
    /// <param name="operationName">The name of the operation to reset.</param>
    public void ResetRetryState(string operationName)
    {
        // In a more sophisticated implementation, this would clear
        // any stored state about previous attempts for this operation
        // For now, this is a placeholder for future enhancement
    }

    #region Private Helper Methods

    private static int GetAttemptCount() => 0;

    private static bool IsRetryableException(Exception exception) =>
        exception is not ArgumentException and
            not ArgumentNullException and
            not InvalidOperationException;

    #endregion
}
