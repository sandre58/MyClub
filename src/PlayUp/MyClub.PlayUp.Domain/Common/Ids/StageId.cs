// -----------------------------------------------------------------------
// <copyright file="StageId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Strongly typed identifier for a Stage aggregate.
/// </summary>
public readonly record struct StageId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageId"/> struct.
    /// </summary>
    /// <param name="value">The underlying GUID value (must not be empty).</param>
    public StageId(Guid value) => Value = TypedId.EnsureNotEmpty(value, nameof(StageId));

    /// <summary>
    /// Gets the underlying GUID value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new identifier using a GUID version 7.
    /// </summary>
    /// <returns>A new non-empty typed identifier.</returns>
    public static StageId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
