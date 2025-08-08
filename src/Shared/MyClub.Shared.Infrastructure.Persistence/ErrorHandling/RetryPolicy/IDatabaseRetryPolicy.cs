// -----------------------------------------------------------------------
// <copyright file="IDatabaseRetryPolicy.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyClub.Shared.Infrastructure.Persistence.ErrorHandling.RetryPolicy;

/// <summary>
/// Defines the contract for database retry policies that determine when and how
/// to retry failed database operations based on exception types and attempt history.
/// </summary>
public interface IDatabaseRetryPolicy
{
    /// <summary>
    /// Calculates the delay before the next retry attempt based on the exception type,
    /// operation context, and retry attempt history.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="operationName">The name of the operation being retried.</param>
    /// <param name="cancellationToken">Token to cancel the delay calculation.</param>
    /// <returns>A task representing the calculated delay duration.</returns>
    Task<TimeSpan> CalculateDelayAsync(Exception exception, string operationName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a retry should be attempted for the given exception and attempt count.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="attemptCount">The number of attempts made so far (0-based).</param>
    /// <returns>True if a retry should be attempted; otherwise, false.</returns>
    bool ShouldRetry(Exception exception, int attemptCount);

    /// <summary>
    /// Resets the retry state for a specific operation, clearing any accumulated backoff or failure history.
    /// </summary>
    /// <param name="operationName">The name of the operation to reset.</param>
    void ResetRetryState(string operationName);
}
