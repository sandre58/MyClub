// -----------------------------------------------------------------------
// <copyright file="CompetitionName.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions;

/// <summary>
/// Display name of a competition.
/// </summary>
public sealed record CompetitionName
{
    /// <summary>
    /// Maximum allowed length after trim.
    /// </summary>
    public const int MaxLength = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionName"/> class.
    /// </summary>
    /// <param name="value">The raw name; trimmed and validated.</param>
    public CompetitionName(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var trimmed = value.Trim();
        Value = trimmed.Length switch
        {
            0 => throw new DomainException("Competition name cannot be empty.", CompetitionErrorCodes.NameInvalid),
            > MaxLength => throw new DomainException(
                $"Competition name cannot exceed {MaxLength} characters.",
                CompetitionErrorCodes.NameInvalid),
            _ => trimmed
        };
    }

    /// <summary>
    /// Gets the normalized competition name.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}
