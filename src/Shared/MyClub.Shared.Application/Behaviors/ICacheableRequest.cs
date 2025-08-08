// -----------------------------------------------------------------------
// <copyright file="ICacheableRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace MyClub.Shared.Application.Behaviors;

/// <summary>
/// Interface for requests that support caching.
/// Implement this interface on queries that should be cached to improve performance.
/// </summary>
public interface ICacheableRequest
{
    /// <summary>
    /// Gets the cache key used to store and retrieve the cached response.
    /// This key should be unique for each distinct request parameter combination.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Gets the cache expiration time.
    /// After this duration, the cached entry will be automatically removed.
    /// </summary>
    TimeSpan? CacheExpiration { get; }
}
