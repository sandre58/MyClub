// -----------------------------------------------------------------------
// <copyright file="PerformanceBehavior.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MyClub.Shared.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that monitors and logs performance metrics for request processing.
/// This behavior measures execution time and logs warnings when requests exceed configured thresholds.
/// It helps identify performance bottlenecks and slow operations that may need optimization.
/// </summary>
/// <typeparam name="TRequest">The type of request being processed.</typeparam>
/// <typeparam name="TResponse">The type of response being returned.</typeparam>
/// <param name="logger">The logger instance for writing performance log entries.</param>
/// <param name="warningThresholdMs">The threshold in milliseconds above which a warning is logged (default: 500ms).</param>
/// <param name="errorThresholdMs">The threshold in milliseconds above which an error is logged (default: 2000ms).</param>
public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
    int warningThresholdMs = 500,
    int errorThresholdMs = 2000) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Action<ILogger, string, long, int, Exception?> LogPerformanceCritical =
        LoggerMessage.Define<string, long, int>(
            LogLevel.Error,
            new(1, nameof(LogPerformanceCritical)),
            "PERFORMANCE CRITICAL: Request {RequestName} took {ElapsedMs}ms to complete (threshold: {ThresholdMs}ms)");

    private static readonly Action<ILogger, string, long, int, Exception?> LogPerformanceWarning =
        LoggerMessage.Define<string, long, int>(
            LogLevel.Warning,
            new(2, nameof(LogPerformanceWarning)),
            "PERFORMANCE WARNING: Request {RequestName} took {ElapsedMs}ms to complete (threshold: {ThresholdMs}ms)");

    private static readonly Action<ILogger, string, long, Exception?> LogRequestCompleted =
        LoggerMessage.Define<string, long>(
            LogLevel.Debug,
            new(3, nameof(LogRequestCompleted)),
            "Request {RequestName} completed in {ElapsedMs}ms");

    /// <summary>
    /// Handles the performance monitoring in the MediatR pipeline.
    /// Measures execution time and logs performance warnings or errors based on configured thresholds.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The response from the next handler, with performance monitoring side effects.</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        var response = await next(cancellationToken).ConfigureAwait(false);

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        // Log performance metrics based on thresholds using LoggerMessage delegates
        if (elapsedMs >= errorThresholdMs)
        {
            LogPerformanceCritical(logger, requestName, elapsedMs, errorThresholdMs, null);
        }
        else if (elapsedMs >= warningThresholdMs)
        {
            LogPerformanceWarning(logger, requestName, elapsedMs, warningThresholdMs, null);
        }
        else
        {
            LogRequestCompleted(logger, requestName, elapsedMs, null);
        }

        return response;
    }
}
