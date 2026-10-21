// -----------------------------------------------------------------------
// <copyright file="StageName.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Display name of a stage.
/// </summary>
public sealed record StageName
{
    /// <summary>
    /// Maximum allowed length after trim.
    /// </summary>
    public const int MaxLength = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="StageName"/> class.
    /// </summary>
    /// <param name="value">The raw name; trimmed and validated.</param>
    public StageName(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var trimmed = value.Trim();
        Value = trimmed.Length switch
        {
            0 => throw new DomainException("Stage name cannot be empty.", StageErrorCodes.NameInvalid),
            > MaxLength => throw new DomainException(
                $"Stage name cannot exceed {MaxLength} characters.",
                StageErrorCodes.NameInvalid),
            _ => trimmed
        };
    }

    /// <summary>
    /// Gets the normalized stage name.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}
