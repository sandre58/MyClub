// -----------------------------------------------------------------------
// <copyright file="CorrelationId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace MyClub.PlayUp.Host;

/// <summary>
/// HTTP correlation-id constants and validation for Play'Up Host.
/// </summary>
internal static partial class CorrelationId
{
    /// <summary>
    /// Gets the request/response header name.
    /// </summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>
    /// Gets the <see cref="HttpContext.Items"/> key used to store the resolved id.
    /// </summary>
    public const string ItemKey = "PlayUp.CorrelationId";

    private const int MaxLength = 128;

    /// <summary>
    /// Creates a new correlation id (Guid format <c>D</c>).
    /// </summary>
    /// <returns>A new correlation id.</returns>
    public static string NewId() => Guid.NewGuid().ToString("D");

    /// <summary>
    /// Returns whether <paramref name="value"/> is an acceptable inbound correlation id.
    /// </summary>
    /// <param name="value">Candidate header value.</param>
    /// <returns><see langword="true"/> when the value may be echoed; otherwise <see langword="false"/>.</returns>
    public static bool IsValid([NotNullWhen(true)] string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= MaxLength
        && AllowedPattern().IsMatch(value);

    [GeneratedRegex("^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex AllowedPattern();
}
