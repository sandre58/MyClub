// -----------------------------------------------------------------------
// <copyright file="LogoUri.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions;

/// <summary>
/// Optional logo reference: app-relative path (<c>/...</c>) or absolute http(s) URI.
/// </summary>
public sealed record LogoUri
{
    /// <summary>
    /// Maximum allowed length after trim.
    /// </summary>
    public const int MaxLength = 2048;

    private LogoUri(string value) => Value = value;

    /// <summary>
    /// Gets the normalized logo URI or path.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a logo URI, or <see langword="null"/> when the input is null/whitespace.
    /// </summary>
    /// <param name="value">Raw value; trimmed.</param>
    /// <returns>The logo URI, or <see langword="null"/>.</returns>
    public static LogoUri? Create(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length switch
        {
            0 => null,
            > MaxLength => throw new DomainException($"Logo URI cannot exceed {MaxLength} characters.",
                CompetitionErrorCodes.InvalidLogoUri),
            _ => trimmed.StartsWith('/') || (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute) &&
                                             (absolute.Scheme == Uri.UriSchemeHttp ||
                                              absolute.Scheme == Uri.UriSchemeHttps))
                ? new LogoUri(trimmed)
                : throw new DomainException(
                    "Logo URI must be an app-relative path starting with '/' or an absolute http(s) URI.",
                    CompetitionErrorCodes.InvalidLogoUri)
        };
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
