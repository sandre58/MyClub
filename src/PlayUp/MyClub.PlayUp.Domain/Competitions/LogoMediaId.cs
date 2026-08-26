// -----------------------------------------------------------------------
// <copyright file="LogoMediaId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions;

/// <summary>
/// Local Play'up reference to a Media item (Guid only — no dependency on Media.Domain).
/// </summary>
public readonly record struct LogoMediaId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogoMediaId"/> struct.
    /// </summary>
    /// <param name="value">The underlying GUID value (must not be empty).</param>
    public LogoMediaId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                $"{nameof(LogoMediaId)} cannot be empty.",
                CompetitionErrorCodes.InvalidLogoMediaId);
        }

        Value = value;
    }

    /// <summary>
    /// Gets the underlying GUID value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a logo media reference, or <see langword="null"/> when the input is null.
    /// </summary>
    /// <param name="value">Optional Guid from the wire or persistence.</param>
    /// <returns>The typed id, or <see langword="null"/>.</returns>
    public static LogoMediaId? Create(Guid? value) =>
        value is null ? null : new LogoMediaId(value.Value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
