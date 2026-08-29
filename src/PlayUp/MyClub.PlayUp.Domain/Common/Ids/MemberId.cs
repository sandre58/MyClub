// -----------------------------------------------------------------------
// <copyright file="MemberId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Strongly typed identifier for a <c>DeclaredMember</c> within a competition entry declaration.
/// Local to that participation — not a global person identity.
/// </summary>
public readonly record struct MemberId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberId"/> struct.
    /// </summary>
    /// <param name="value">The underlying GUID value (must not be empty).</param>
    public MemberId(Guid value) => Value = TypedId.EnsureNotEmpty(value, nameof(MemberId));

    /// <summary>
    /// Gets the underlying GUID value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new identifier using a GUID version 7.
    /// </summary>
    /// <returns>A new non-empty typed identifier.</returns>
    public static MemberId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
