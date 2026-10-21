// -----------------------------------------------------------------------
// <copyright file="DisciplinaryEventId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Strongly typed identifier for a <c>RecordedDisciplinaryEvent</c> within a match.
/// </summary>
public readonly record struct DisciplinaryEventId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DisciplinaryEventId"/> struct.
    /// </summary>
    /// <param name="value">The underlying GUID value (must not be empty).</param>
    public DisciplinaryEventId(Guid value) =>
        Value = TypedId.EnsureNotEmpty(value, nameof(DisciplinaryEventId));

    /// <summary>
    /// Gets the underlying GUID value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new identifier using a GUID version 7.
    /// </summary>
    public static DisciplinaryEventId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
