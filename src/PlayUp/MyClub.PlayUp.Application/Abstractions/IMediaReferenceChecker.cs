// -----------------------------------------------------------------------
// <copyright file="IMediaReferenceChecker.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Abstractions;

/// <summary>
/// Application port: verifies that a Media Guid exists (no cross-schema FK).
/// </summary>
public interface IMediaReferenceChecker
{
    /// <summary>
    /// Returns whether a Media item exists for the given identity.
    /// </summary>
    /// <param name="mediaId">Media Guid.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the media exists.</returns>
    Task<bool> ExistsAsync(Guid mediaId, CancellationToken cancellationToken = default);
}
