// -----------------------------------------------------------------------
// <copyright file="MediaReferenceChecker.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Media.Application.Abstractions;
using MyClub.Media.Domain;
using MyClub.PlayUp.Application.Abstractions;

namespace MyClub.PlayUp.Host;

/// <summary>
/// Resolves Play'up logo Media references against the Media repository (composition root).
/// </summary>
internal sealed class MediaReferenceChecker(IMediaRepository mediaRepository) : IMediaReferenceChecker
{
    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        var media = await mediaRepository
            .GetByIdAsync(new MediaId(mediaId), cancellationToken)
            .ConfigureAwait(false);
        return media is not null;
    }
}
