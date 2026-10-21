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

    /// <summary>
    /// Gets the media identity.
    /// </summary>
    public MediaId Id { get; }

    /// <summary>
    /// Gets the validated content type (allowlisted string).
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Gets the payload size in bytes.
    /// </summary>
    public long ByteSize { get; }

    /// <summary>
    /// Gets the internal storage key (never expose to product domains as a public URL).
    /// </summary>
    public string StorageKey { get; }

    /// <summary>
    /// Gets the optional sanitized original file name.
    /// </summary>
    public string? OriginalName { get; }

    /// <summary>
    /// Gets the creation timestamp (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

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
