// -----------------------------------------------------------------------
// <copyright file="LoggingBehavior.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using MyClub.Shared.Application.Abstractions.Monitoring;

namespace MyClub.Shared.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that provides comprehensive logging for all requests and responses.
/// This behavior logs request details, execution time, and response information for monitoring and debugging purposes.
/// It helps track application usage patterns, performance bottlenecks, and potential issues.
/// </summary>
/// <typeparam name="TRequest">The type of request being processed.</typeparam>
/// <typeparam name="TResponse">The type of response being returned.</typeparam>
/// <param name="logger">The logger instance for writing log entries.</param>
/// <param name="metrics">The metrics instance for recording persistence operation statistics.</param>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger, IPersistenceMetrics metrics) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Action<ILogger, string, Guid, string, Exception?> LogProcessingRequest =
        LoggerMessage.Define<string, Guid, string>(
            LogLevel.Information,
            new(1, nameof(LogProcessingRequest)),
            "Processing request {RequestName} with ID {RequestId}. Request: {Request}");

    private static readonly Action<ILogger, string, Guid, long, string, Exception?> LogRequestCompleted =
        LoggerMessage.Define<string, Guid, long, string>(
            LogLevel.Information,
            new(2, nameof(LogRequestCompleted)),
            "Request {RequestName} with ID {RequestId} completed successfully in {ElapsedMs}ms. Response: {Response}");

    private static readonly Action<ILogger, string, Guid, long, string, Exception?> LogRequestFailed =
        LoggerMessage.Define<string, Guid, long, string>(
            LogLevel.Error,
            new(3, nameof(LogRequestFailed)),
            "Request {RequestName} with ID {RequestId} failed after {ElapsedMs}ms. Error: {ErrorMessage}");

    /// <summary>
    /// Handles the request logging in the MediatR pipeline.
    /// Logs the incoming request, measures execution time, and logs the response details.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The response from the next handler, with logging side effects.</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid();

        // Log incoming request using LoggerMessage delegate
        LogProcessingRequest(logger, requestName, requestId, JsonSerializer.Serialize(request), null);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            // Log successful response using LoggerMessage delegate
            LogRequestCompleted(logger, requestName, requestId, stopwatch.ElapsedMilliseconds, JsonSerializer.Serialize(response), null);
            metrics.RecordOperationDuration(requestName, stopwatch.Elapsed, true);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log exception using LoggerMessage delegate
            LogRequestFailed(logger, requestName, requestId, stopwatch.ElapsedMilliseconds, ex.Message, ex);
            metrics.RecordOperationDuration(requestName, stopwatch.Elapsed, false);
            metrics.RecordDatabaseError(requestName, ex.GetType().Name, false);

            throw;
        }
    }
}
