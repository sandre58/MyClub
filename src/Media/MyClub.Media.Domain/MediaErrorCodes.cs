// -----------------------------------------------------------------------
// <copyright file="MediaErrorCodes.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Media.Domain;

/// <summary>
/// Stable machine-readable codes for Media domain failures.
/// </summary>
public static class MediaErrorCodes
{
    /// <summary>
    /// Content type is missing or not in the allowlist.
    /// </summary>
    public const string InvalidContentType = "Media.InvalidContentType";

    /// <summary>
    /// Payload exceeds the maximum allowed size.
    /// </summary>
    public const string PayloadTooLarge = "Media.PayloadTooLarge";

    /// <summary>
    /// Byte size is zero or negative.
    /// </summary>
    public const string InvalidByteSize = "Media.InvalidByteSize";

    /// <summary>
    /// Media identity is empty.
    /// </summary>
    public const string InvalidMediaId = "Media.InvalidMediaId";

    /// <summary>
    /// Storage key is missing or invalid.
    /// </summary>
    public const string InvalidStorageKey = "Media.InvalidStorageKey";

    /// <summary>
    /// Original file name exceeds the allowed length.
    /// </summary>
    public const string InvalidOriginalName = "Media.InvalidOriginalName";
}
