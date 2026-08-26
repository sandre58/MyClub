// -----------------------------------------------------------------------
// <copyright file="IMediaStorage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Media.Application.Abstractions;

/// <summary>
/// Binary storage port for Media payloads (filesystem, blob, …).
/// </summary>
public interface IMediaStorage
{
    /// <summary>
    /// Saves binary content under the given storage key.
    /// </summary>
    /// <param name="storageKey">Internal storage key.</param>
    /// <param name="content">Payload stream.</param>
    /// <param name="contentType">Validated content type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the payload is stored.</returns>
    Task SaveAsync(
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a readable stream for the stored payload.
    /// </summary>
    /// <param name="storageKey">Internal storage key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A readable stream owned by the caller.</returns>
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the stored payload if it exists.
    /// </summary>
    /// <param name="storageKey">Internal storage key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see langword="true"/> when a file was deleted;
    /// <see langword="false"/> when the key was already missing.
    /// </returns>
    Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
