// -----------------------------------------------------------------------
// <copyright file="MediaService.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Media.Application.Abstractions;
using MyClub.Media.Domain;

namespace MyClub.Media.Application.Media;

/// <summary>
/// Orchestrates Media metadata persistence and binary storage.
/// </summary>
/// <remarks>
/// Upload order: validate → create aggregate → save bytes → save metadata;
/// on metadata failure after a successful storage write, the use case deletes the orphan file.
/// Delete order: load → remove metadata + save → delete file (missing file = success;
/// filesystem failure after metadata commit = operational error).
/// </remarks>
public sealed class MediaService(IMediaRepository repository, IMediaStorage storage)
{
    /// <summary>
    /// Creates a Media item from an uploaded payload.
    /// </summary>
    /// <param name="content">Payload stream.</param>
    /// <param name="contentType">Declared content type.</param>
    /// <param name="byteSize">Declared payload size.</param>
    /// <param name="originalName">Optional original file name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Persisted metadata.</returns>
    public async Task<MediaMetadataDto> CreateAsync(
        Stream content,
        string contentType,
        long byteSize,
        string? originalName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var media = MediaItem.Create(
            contentType,
            byteSize,
            originalName,
            DateTimeOffset.UtcNow);

        await storage
            .SaveAsync(media.StorageKey, content, media.ContentType, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            repository.Add(media);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            try
            {
                await storage.DeleteAsync(media.StorageKey, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Best-effort cleanup; rethrow the original persistence failure.
            }

            throw;
        }

        return ToDto(media);
    }

    /// <summary>
    /// Loads Media metadata by identity.
    /// </summary>
    /// <param name="id">Media identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Metadata DTO.</returns>
    public async Task<MediaMetadataDto> GetMetadataAsync(MediaId id, CancellationToken cancellationToken = default)
    {
        var media = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Media '{id}' was not found.",
                        MediaApplicationErrorCodes.MediaNotFound);

        return ToDto(media);
    }

    /// <summary>
    /// Opens Media binary content by identity.
    /// </summary>
    /// <param name="id">Media identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Opened content (caller disposes the stream).</returns>
    public async Task<MediaContentResult> OpenContentAsync(MediaId id, CancellationToken cancellationToken = default)
    {
        var media = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Media '{id}' was not found.",
                        MediaApplicationErrorCodes.MediaNotFound);

        try
        {
            var stream = await storage
                .OpenReadAsync(media.StorageKey, cancellationToken)
                .ConfigureAwait(false);

            return new MediaContentResult(stream, media.ContentType, media.ByteSize, media.OriginalName);
        }
        catch (FileNotFoundException exception)
        {
            throw new ApplicationFailureException(
                $"Media '{id}' content is unavailable.",
                MediaApplicationErrorCodes.ContentUnavailable,
                exception);
        }
    }

    /// <summary>
    /// Deletes Media metadata then binary content.
    /// </summary>
    /// <param name="id">Media identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when deletion succeeds.</returns>
    public async Task DeleteAsync(MediaId id, CancellationToken cancellationToken = default)
    {
        var media = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Media '{id}' was not found.",
                        MediaApplicationErrorCodes.MediaNotFound);

        var storageKey = media.StorageKey;
        repository.Remove(media);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _ = await storage.DeleteAsync(storageKey, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ApplicationFailureException(
                $"Media '{id}' metadata was deleted but the stored file could not be removed.",
                MediaApplicationErrorCodes.StorageDeleteFailed,
                exception);
        }
    }

    private static MediaMetadataDto ToDto(MediaItem media) =>
        new(media.Id.Value, media.ContentType, media.ByteSize, media.OriginalName, media.CreatedAt);
}
