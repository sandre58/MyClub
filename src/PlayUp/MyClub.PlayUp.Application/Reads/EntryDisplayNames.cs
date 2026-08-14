// -----------------------------------------------------------------------
// <copyright file="EntryDisplayNames.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Resolves entry display names from a Competition (outside Domain).
/// </summary>
internal static class EntryDisplayNames
{
    /// <summary>
    /// Builds a lookup of entry identity → display name.
    /// </summary>
    public static IReadOnlyDictionary<EntryId, string> ToMap(Competition competition)
    {
        ArgumentNullException.ThrowIfNull(competition);
        return competition.Entries.ToDictionary(entry => entry.Id, entry => entry.DisplayName);
    }

    /// <summary>
    /// Resolves a display name, or <see langword="null"/> when the entry is unknown.
    /// </summary>
    public static string? Resolve(IReadOnlyDictionary<EntryId, string> names, EntryId entryId) =>
        names.GetValueOrDefault(entryId);
}
