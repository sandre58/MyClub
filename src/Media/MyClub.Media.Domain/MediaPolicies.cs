// -----------------------------------------------------------------------
// <copyright file="MediaPolicies.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Media.Domain;

/// <summary>
/// Provisional Media size and naming policies (v1 — subject to future product arbitration).
/// </summary>
public static class MediaPolicies
{
    /// <summary>
    /// Maximum payload size in bytes (2 MiB).
    /// </summary>
    public const long MaxByteSize = 2L * 1024 * 1024;

    /// <summary>
    /// Maximum length of the sanitized original file name.
    /// </summary>
    public const int MaxOriginalNameLength = 255;

    /// <summary>
    /// Validates that <paramref name="byteSize"/> is within allowed bounds.
    /// </summary>
    /// <param name="byteSize">Payload size in bytes.</param>
    public static void EnsureByteSizeAllowed(long byteSize)
    {
        switch (byteSize)
        {
            case <= 0:
                throw new DomainException(
                    "Media byte size must be greater than zero.",
                    MediaErrorCodes.InvalidByteSize);
            case > MaxByteSize:
                throw new DomainException(
                    $"Media payload exceeds the maximum of {MaxByteSize} bytes.",
                    MediaErrorCodes.PayloadTooLarge);
        }
    }

    /// <summary>
    /// Sanitizes an optional original file name for metadata storage.
    /// </summary>
    /// <param name="originalName">Caller-supplied file name.</param>
    /// <returns>A safe file name, or <see langword="null"/>.</returns>
    public static string? SanitizeOriginalName(string? originalName)
    {
        if (string.IsNullOrWhiteSpace(originalName))
        {
            return null;
        }

        var trimmed = Path.GetFileName(originalName.Trim());
        return string.IsNullOrWhiteSpace(trimmed)
            ? null
            : trimmed.Length > MaxOriginalNameLength
            ? throw new DomainException(
                $"Original file name cannot exceed {MaxOriginalNameLength} characters.",
                MediaErrorCodes.InvalidOriginalName)
            : trimmed;
    }
}
