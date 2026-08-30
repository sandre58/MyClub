// -----------------------------------------------------------------------
// <copyright file="SubstitutionId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Strongly typed identifier for a <c>RecordedSubstitution</c> within a match.
/// </summary>
public readonly record struct SubstitutionId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubstitutionId"/> struct.
    /// </summary>
    /// <param name="value">The underlying GUID value (must not be empty).</param>
    public SubstitutionId(Guid value) => Value = TypedId.EnsureNotEmpty(value, nameof(SubstitutionId));

    /// <summary>
    /// Gets the underlying GUID value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new identifier using a GUID version 7.
    /// </summary>
    /// <returns>A new non-empty typed identifier.</returns>
    public static SubstitutionId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
