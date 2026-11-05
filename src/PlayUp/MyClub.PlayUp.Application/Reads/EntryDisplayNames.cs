// -----------------------------------------------------------------------
// <copyright file="EntryDisplayNames.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Resolves entry display presentation from a Competition (outside Domain).
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
    /// Builds a lookup of entry identity → presentation snapshot.
    /// </summary>
    public static IReadOnlyDictionary<EntryId, CompetitionEntry> ToEntries(Competition competition)
    {
        ArgumentNullException.ThrowIfNull(competition);
        return competition.Entries.ToDictionary(entry => entry.Id);
    }

    /// <summary>
    /// Resolves a display name, or <see langword="null"/> when the entry is unknown.
    /// </summary>
    public static string? Resolve(IReadOnlyDictionary<EntryId, string> names, EntryId entryId) =>
        names.GetValueOrDefault(entryId);

    /// <summary>
    /// Resolves a display name. Returns null when the side has no entry yet.
    /// </summary>
    public static string? Resolve(IReadOnlyDictionary<EntryId, string> names, EntryId? entryId) =>
        entryId is { } id ? Resolve(names, id) ?? id.Value.ToString() : null;

    /// <summary>
    /// Builds an <see cref="EntrySideDto"/> from a presentation map.
    /// </summary>
    public static EntrySideDto ToSide(
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries,
        EntryId? entryId,
        string? sourceSlotKey = null)
    {
        if (entryId is not { } id)
        {
            return new EntrySideDto(null, null, SourceSlotKey: sourceSlotKey);
        }

        return entries.TryGetValue(id, out var entry)
            ? new EntrySideDto(
                entry.Id.Value,
                entry.DisplayName,
                entry.ShortName?.Value,
                entry.LogoMediaId?.Value,
                entry.PrimaryColor?.Value,
                entry.SecondaryColor?.Value,
                sourceSlotKey)
            : new EntrySideDto(id.Value, null, SourceSlotKey: sourceSlotKey);
    }

    /// <summary>
    /// Resolves a declared member display name from a competition entry, or <see langword="null"/> when unknown.
    /// </summary>
    public static string? ResolveMemberDisplayName(
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries,
        EntryId entryId,
        MemberId memberId) =>
        !entries.TryGetValue(entryId, out var entry) ? null : entry.DeclaredMembers.FirstOrDefault(member => member.Id.Equals(memberId))?.DisplayName;
}
