// -----------------------------------------------------------------------
// <copyright file="MediaItem.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace MyClub.Media.Domain;

/// <summary>
/// Aggregate root for a stored media item (metadata only — bytes live behind <c>StorageKey</c>).
/// </summary>
/// <remarks>
/// Named <see cref="MediaItem"/> (not <c>Media</c>) to avoid colliding with the <c>MyClub.Media</c> namespace.
/// </remarks>
[DebuggerDisplay("{Id} ({ContentType}, {ByteSize} bytes)")]
public sealed class MediaItem
{
    private MediaItem(
        MediaId id,
        string contentType,
        long byteSize,
        string storageKey,
        string? originalName,
        DateTimeOffset createdAt)
    {
        Id = id;
        ContentType = contentType;
        ByteSize = byteSize;
        StorageKey = storageKey;
        OriginalName = originalName;
        CreatedAt = createdAt;
    }

    // EF Core materialization.
#pragma warning disable CS8618
    private MediaItem()
    {
    }
#pragma warning restore CS8618

    /// <summary>
    /// Gets the media identity.
    /// </summary>
    public MediaId Id { get; private set; }

    /// <summary>
    /// Gets the validated content type (allowlisted string).
    /// </summary>
    public string ContentType { get; private set; }

    /// <summary>
    /// Gets the payload size in bytes.
    /// </summary>
    public long ByteSize { get; private set; }

    /// <summary>
    /// Gets the internal storage key (never expose to product domains as a public URL).
    /// </summary>
    public string StorageKey { get; private set; }

    /// <summary>
    /// Gets the optional sanitized original file name.
    /// </summary>
    public string? OriginalName { get; private set; }

    /// <summary>
    /// Gets the creation timestamp (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Creates a new media item with a stable storage key derived from the identity.
    /// </summary>
    /// <param name="contentType">Raw content type.</param>
    /// <param name="byteSize">Payload size in bytes.</param>
    /// <param name="originalName">Optional original file name.</param>
    /// <param name="createdAt">Creation timestamp.</param>
    /// <returns>A new media item.</returns>
    public static MediaItem Create(
        string contentType,
        long byteSize,
        string? originalName,
        DateTimeOffset createdAt)
    {
        var id = MediaId.New();
        var normalizedType = MediaContentTypes.NormalizeAndValidate(contentType);
        MediaPolicies.EnsureByteSizeAllowed(byteSize);
        var safeName = MediaPolicies.SanitizeOriginalName(originalName);
        var storageKey = BuildStorageKey(id, normalizedType);

        return new MediaItem(id, normalizedType, byteSize, storageKey, safeName, createdAt);
    }

    /// <summary>
    /// Rebuilds a media item from persistence (no re-validation of allowlist — already stored).
    /// </summary>
    /// <param name="id">Media identity.</param>
    /// <param name="contentType">Stored content type.</param>
    /// <param name="byteSize">Stored byte size.</param>
    /// <param name="storageKey">Stored storage key.</param>
    /// <param name="originalName">Stored original name.</param>
    /// <param name="createdAt">Stored creation timestamp.</param>
    /// <returns>The reconstructed aggregate.</returns>
    public static MediaItem Reconstitute(
        MediaId id,
        string contentType,
        long byteSize,
        string storageKey,
        string? originalName,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new DomainException("Storage key is required.", MediaErrorCodes.InvalidStorageKey);
        }

        return new MediaItem(id, contentType, byteSize, storageKey, originalName, createdAt);
    }

    private static string BuildStorageKey(MediaId id, string contentType)
    {
        var extension = contentType switch
        {
            MediaContentTypes.Png => ".png",
            MediaContentTypes.Jpeg => ".jpg",
            MediaContentTypes.Webp => ".webp",
            _ => string.Empty
        };

        return $"{id.Value:N}{extension}";
    }
}
