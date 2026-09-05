// -----------------------------------------------------------------------
// <copyright file="ShortName.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions;

/// <summary>
/// Abbreviated display name (competition optional; entry required).
/// Auto-derive (spaces only; hyphens stay in the same token):
/// 0 words → <c>T</c>; 1 word → uppercase truncated to <see cref="MaxLength"/>;
/// 2+ words → uppercase initials of the first 3 tokens.
/// </summary>
public sealed record ShortName
{
    /// <summary>
    /// Maximum allowed length after trim.
    /// </summary>
    public const int MaxLength = 5;

    private ShortName(string value) => Value = value;

    /// <summary>
    /// Gets the normalized short name.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a short name, or <see langword="null"/> when the input is null/whitespace.
    /// </summary>
    /// <param name="value">Raw value; trimmed.</param>
    /// <returns>The short name, or <see langword="null"/>.</returns>
    public static ShortName? Create(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length switch
        {
            0 => null,
            > MaxLength => throw new DomainException($"Short name cannot exceed {MaxLength} characters.",
                CompetitionErrorCodes.InvalidShortName),
            _ => new ShortName(trimmed)
        };
    }

    /// <summary>
    /// Creates a required short name (entry presentation).
    /// </summary>
    /// <param name="value">Raw value; trimmed.</param>
    /// <returns>The short name.</returns>
    public static ShortName CreateRequired(string? value) =>
        Create(value) ?? throw new DomainException(
            "Short name cannot be empty.",
            CompetitionErrorCodes.InvalidShortName);

    /// <summary>
    /// Derives a short name from a display name (initials or truncated uppercase).
    /// </summary>
    /// <param name="displayName">Entry or team display name.</param>
    /// <returns>A required short name.</returns>
    public static ShortName FromDisplayName(string displayName)
    {
        ArgumentNullException.ThrowIfNull(displayName);
        var parts = displayName.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var derived = parts.Length switch
        {
            0 => "T",
            1 => parts[0].Length <= MaxLength
                ? parts[0].ToUpperInvariant()
                : parts[0][..MaxLength].ToUpperInvariant(),
            _ => string.Concat(parts.Take(3).Select(part => char.ToUpperInvariant(part[0])))
        };
        return CreateRequired(derived);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
