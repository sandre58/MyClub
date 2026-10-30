// -----------------------------------------------------------------------
// <copyright file="RulesReplaceBodyGuard.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application;

namespace MyClub.PlayUp.Host.Endpoints;

/// <summary>
/// Rejects an ambiguous PUT rules body before the endpoint calls the executor.
/// </summary>
internal static class RulesReplaceBodyGuard
{
    /// <summary>
    /// Accepts either <paramref name="clear"/> with no items, or a non-empty list without <paramref name="clear"/>.
    /// </summary>
    /// <param name="clear">Whether the body asks to clear rules.</param>
    /// <param name="count">Item count of the list, or <see langword="null"/> when the list is absent.</param>
    internal static void EnsureListOrClear(bool clear, int? count)
    {
        var hasNonEmptyList = count is > 0;
        if ((clear && hasNonEmptyList) || (!clear && !hasNonEmptyList))
        {
            throw new ApplicationFailureException(
                "Rules replace requires a non-empty list or clear: true, and not both.",
                ApplicationErrorCodes.RulesReplaceRequiresListOrClear);
        }
    }
}
