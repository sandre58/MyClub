// -----------------------------------------------------------------------
// <copyright file="DrawId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Strongly typed identifier for a Draw entity.
/// </summary>
public readonly record struct DrawId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DrawId"/> struct.
    /// </summary>
    /// <param name="value">The underlying GUID value (must not be empty).</param>
    public DrawId(Guid value) => Value = TypedId.EnsureNotEmpty(value, nameof(DrawId));

    /// <summary>
    /// Gets the underlying GUID value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new identifier using a GUID version 7.
    /// </summary>
    /// <returns>A new non-empty typed identifier.</returns>
    public static DrawId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
