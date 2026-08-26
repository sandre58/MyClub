// -----------------------------------------------------------------------
// <copyright file="IMediaRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Media.Domain;

namespace MyClub.Media.Application.Abstractions;

/// <summary>
/// Persistence port for Media metadata (not binary bytes).
/// </summary>
public interface IMediaRepository
{
    /// <summary>
    /// Loads media metadata by identity, or <see langword="null"/> if missing.
    /// </summary>
    /// <param name="id">Media identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The media item, or <see langword="null"/>.</returns>
    Task<MediaItem?> GetByIdAsync(MediaId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds media metadata to the current unit of work.
    /// </summary>
    /// <param name="media">The media item.</param>
    void Add(MediaItem media);

    /// <summary>
    /// Removes media metadata from the current unit of work.
    /// </summary>
    /// <param name="media">The media item.</param>
    void Remove(MediaItem media);

    /// <summary>
    /// Persists pending metadata changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
