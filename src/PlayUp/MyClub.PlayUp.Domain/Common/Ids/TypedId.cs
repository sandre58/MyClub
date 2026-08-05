// -----------------------------------------------------------------------
// <copyright file="TypedId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Shared validation helpers for typed identifiers.
/// </summary>
internal static class TypedId
{
    /// <summary>
    /// Ensures the given value is not <see cref="Guid.Empty"/>.
    /// </summary>
    /// <param name="value">The identifier value.</param>
    /// <param name="name">The typed id type name used in the error message.</param>
    /// <returns>The validated value.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is empty.</exception>
    public static Guid EnsureNotEmpty(Guid value, string name) =>
        value == Guid.Empty
            ? throw new DomainException($"{name} cannot be empty.")
            : value;
}
