// -----------------------------------------------------------------------
// <copyright file="AssociationId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Strongly typed identifier for an association reference (external identity).
/// Not an Association aggregate — used only as a map value for draw constraints.
/// </summary>
public readonly record struct AssociationId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssociationId"/> struct.
    /// </summary>
    /// <param name="value">The underlying GUID value (must not be empty).</param>
    public AssociationId(Guid value) => Value = TypedId.EnsureNotEmpty(value, nameof(AssociationId));

    /// <summary>
    /// Gets the underlying GUID value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new identifier using a GUID version 7.
    /// </summary>
    /// <returns>A new non-empty typed identifier.</returns>
    public static AssociationId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
