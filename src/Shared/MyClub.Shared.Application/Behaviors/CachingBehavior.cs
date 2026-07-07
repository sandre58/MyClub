// -----------------------------------------------------------------------
// <copyright file="CachingBehavior.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MyClub.Shared.Application.Abstractions.Monitoring;

namespace MyClub.Shared.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that provides caching capabilities for queries.
/// This behavior caches responses for requests that implement ICacheableRequest,
/// reducing database load and improving response times for frequently accessed data.
/// </summary>
/// <typeparam name="TRequest">The type of request being processed.</typeparam>
/// <typeparam name="TResponse">The type of response being cached.</typeparam>
/// <param name="cache">The memory cache instance for storing responses.</param>
/// <param name="logger">The logger instance for cache-related log entries.</param>
/// <param name="metrics">The persistence metrics instance for recording cache operations.</param>
public sealed class CachingBehavior<TRequest, TResponse>(
    IMemoryCache cache,
    ILogger<CachingBehavior<TRequest, TResponse>> logger,
    IPersistenceMetrics metrics) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(5);

    private static readonly Action<ILogger, string, Exception?> LogCacheHit =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new(1, nameof(LogCacheHit)),
            "Cache hit for key: {CacheKey}");

    private static readonly Action<ILogger, string, Exception?> LogCacheMiss =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new(2, nameof(LogCacheMiss)),
            "Cache miss for key: {CacheKey}");

    private static readonly Action<ILogger, string, TimeSpan, Exception?> LogResponseCached =
        LoggerMessage.Define<string, TimeSpan>(
            LogLevel.Debug,
            new(3, nameof(LogResponseCached)),
            "Cached response for key: {CacheKey} with expiration: {Expiration}");

    /// <summary>
    /// Handles the caching logic in the MediatR pipeline.
    /// Attempts to retrieve cached responses for cacheable requests,
    /// or executes the request and caches the result if not found.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The cached response if available, otherwise the response from the next handler (which is then cached).</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only cache requests that implement ICacheableRequest
        if (request is not ICacheableRequest cacheableRequest)
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        var cacheKey = cacheableRequest.CacheKey;

        // Try to get from cache
        if (cache.TryGetValue(cacheKey, out TResponse? cachedResponse))
        {
            LogCacheHit(logger, cacheKey, null);
            metrics.RecordOperationDuration($"CacheHit:{cacheKey}", TimeSpan.Zero, true);
            return cachedResponse!;
        }

        LogCacheMiss(logger, cacheKey, null);

        // Execute request and cache the result
        var response = await next(cancellationToken).ConfigureAwait(false);

        var expiration = cacheableRequest.CacheExpiration ?? DefaultCacheExpiration;
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
            Size = EstimateSize(response)
        };

        cache.Set(cacheKey, response, cacheEntryOptions);

        LogResponseCached(logger, cacheKey, expiration, null);
        metrics.RecordOperationDuration($"CacheSet:{cacheKey}", TimeSpan.Zero, true);

        return response;
    }

    /// <summary>
    /// Estimates the size of the response for cache management.
    /// This is a simple estimation based on JSON serialization length.
    /// </summary>
    /// <param name="response">The response to estimate size for.</param>
    /// <returns>Estimated size in arbitrary units for cache memory management.</returns>
    private static long EstimateSize(TResponse response)
    {
        try
        {
            var json = JsonSerializer.Serialize(response);
            return json.Length;
        }
        catch
        {
            // Fallback to a default size if serialization fails
            return 1024;
        }
    }
}
