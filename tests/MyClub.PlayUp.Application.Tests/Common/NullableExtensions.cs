// -----------------------------------------------------------------------
// <copyright file="NullableExtensions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Tests.Common;

/// <summary>
/// Test helper formerly provided by MyNet.Primitives (Domain no longer references MyNet).
/// </summary>
internal static class NullableExtensions
{
    /// <summary>
    /// Returns <paramref name="value"/> or throws when null.
    /// </summary>
    /// <typeparam name="T">Reference type.</typeparam>
    /// <param name="value">Possibly null value.</param>
    /// <param name="paramName">Optional parameter name for the exception.</param>
    /// <returns>The non-null value.</returns>
    public static T OrThrow<T>(this T? value, string? paramName = null)
        where T : class =>
        value ?? throw new InvalidOperationException(
            paramName is null
                ? "Expected a non-null value."
                : $"Expected a non-null value for '{paramName}'.");
}
