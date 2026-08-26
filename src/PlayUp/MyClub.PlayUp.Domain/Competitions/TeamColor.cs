// -----------------------------------------------------------------------
// <copyright file="TeamColor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions;

/// <summary>
/// Optional team kit color as <c>#RRGGBB</c> (uppercase).
/// </summary>
public sealed partial record TeamColor
{
    private TeamColor(string value) => Value = value;

    /// <summary>
    /// Gets the normalized <c>#RRGGBB</c> color.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a team color, or <see langword="null"/> when the input is null/whitespace.
    /// </summary>
    /// <param name="value">Raw hex color.</param>
    /// <returns>The color, or <see langword="null"/>.</returns>
    public static TeamColor? Create(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (!HexColorRegex().IsMatch(trimmed))
        {
            throw new DomainException(
                "Team color must be a #RRGGBB hex value.",
                CompetitionErrorCodes.InvalidTeamColor);
        }

        return new TeamColor(trimmed.ToUpperInvariant());
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexColorRegex();
}
