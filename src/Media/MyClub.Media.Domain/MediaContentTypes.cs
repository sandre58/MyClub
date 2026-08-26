// -----------------------------------------------------------------------
// <copyright file="MediaContentTypes.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Media.Domain;

/// <summary>
/// Provisional allowlist of Media content types (v1 product policy — not a formal Décision yet).
/// </summary>
public static class MediaContentTypes
{
    /// <summary>
    /// PNG image.
    /// </summary>
    public const string Png = "image/png";

    /// <summary>
    /// JPEG image.
    /// </summary>
    public const string Jpeg = "image/jpeg";

    /// <summary>
    /// WebP image.
    /// </summary>
    public const string Webp = "image/webp";

    /// <summary>
    /// Gets the allowed content types for Media v1.
    /// </summary>
    public static IReadOnlySet<string> Allowed { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Png, Jpeg, Webp };

    /// <summary>
    /// Normalizes and validates a content type against the allowlist.
    /// </summary>
    /// <param name="contentType">Raw content type (may include parameters).</param>
    /// <returns>The normalized media type (lowercase type/subtype).</returns>
    public static string NormalizeAndValidate(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new DomainException(
                "Content type is required.",
                MediaErrorCodes.InvalidContentType);
        }

        var mediaType = contentType.Split(';', 2)[0].Trim();
        foreach (var allowed in Allowed)
        {
            if (string.Equals(allowed, mediaType, StringComparison.OrdinalIgnoreCase))
            {
                return allowed;
            }
        }

        throw new DomainException(
            $"Content type '{mediaType}' is not allowed. Allowed: {string.Join(", ", Allowed.Order(StringComparer.Ordinal))}.",
            MediaErrorCodes.InvalidContentType);
    }
}
