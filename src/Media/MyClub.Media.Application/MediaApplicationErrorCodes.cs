// -----------------------------------------------------------------------
// <copyright file="MediaApplicationErrorCodes.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Media.Application;

/// <summary>
/// Stable machine-readable codes for Media application failures.
/// </summary>
public static class MediaApplicationErrorCodes
{
    /// <summary>
    /// Media was not found.
    /// </summary>
    public const string MediaNotFound = "Media.NotFound";

    /// <summary>
    /// Binary storage operation failed after metadata was committed (orphan risk).
    /// </summary>
    public const string StorageDeleteFailed = "Media.StorageDeleteFailed";

    /// <summary>
    /// Stored content could not be opened.
    /// </summary>
    public const string ContentUnavailable = "Media.ContentUnavailable";
}
