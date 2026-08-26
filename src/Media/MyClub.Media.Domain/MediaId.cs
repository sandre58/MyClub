// -----------------------------------------------------------------------
// <copyright file="MediaId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Media.Domain;

/// <summary>
/// Strongly typed identifier for a Media aggregate.
/// </summary>
/// <remarks>
/// Lives in <c>MyClub.Media.Domain</c> only. Future product domains may store a
/// <see cref="Guid"/> reference without taking a project dependency on this type.
/// </remarks>
public readonly record struct MediaId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediaId"/> struct.
    /// </summary>
    /// <param name="value">The underlying GUID value (must not be empty).</param>
    public MediaId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{nameof(MediaId)} cannot be empty.", MediaErrorCodes.InvalidMediaId);
        }

        Value = value;
    }

    /// <summary>
    /// Gets the underlying GUID value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new identifier using a GUID version 7.
    /// </summary>
    /// <returns>A new non-empty typed identifier.</returns>
    public static MediaId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
