// -----------------------------------------------------------------------
// <copyright file="MediaRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using MyClub.Media.Application.Abstractions;
using MyClub.Media.Domain;

namespace MyClub.Media.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository for Media metadata.
/// </summary>
internal sealed class MediaRepository(MediaDbContext context) : IMediaRepository
{
    /// <inheritdoc />
    public Task<MediaItem?> GetByIdAsync(MediaId id, CancellationToken cancellationToken = default) =>
        context.Set<MediaItem>().SingleOrDefaultAsync(media => media.Id == id, cancellationToken);

    /// <inheritdoc />
    public void Add(MediaItem media)
    {
        ArgumentNullException.ThrowIfNull(media);
        context.Set<MediaItem>().Add(media);
    }

    /// <inheritdoc />
    public void Remove(MediaItem media)
    {
        ArgumentNullException.ThrowIfNull(media);
        context.Set<MediaItem>().Remove(media);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
