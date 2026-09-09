// -----------------------------------------------------------------------
// <copyright file="CorrelationIdHttpContextExtensions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host;

/// <summary>
/// Reads the correlation id resolved for the current HTTP request.
/// </summary>
internal static class CorrelationIdHttpContextExtensions
{
    /// <summary>
    /// Gets the correlation id for <paramref name="httpContext"/> (middleware value or a fresh id).
    /// </summary>
    /// <param name="httpContext">Current HTTP context.</param>
    /// <returns>The correlation id string.</returns>
    public static string GetCorrelationId(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        if (httpContext.Items.TryGetValue(CorrelationId.ItemKey, out var existing)
            && existing is string { Length: > 0 } id)
        {
            return id;
        }

        var generated = CorrelationId.NewId();
        httpContext.Items[CorrelationId.ItemKey] = generated;
        return generated;
    }
}
