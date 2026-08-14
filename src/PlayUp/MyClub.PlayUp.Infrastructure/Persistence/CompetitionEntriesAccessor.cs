// -----------------------------------------------------------------------
// <copyright file="CompetitionEntriesAccessor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Reads and reorders Competition._entries without exposing a Domain persistence API.
/// </summary>
internal static class CompetitionEntriesAccessor
{
    private static readonly FieldInfo EntriesField = typeof(Competition).GetField(
        "_entries",
        BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Competition._entries backing field was not found.");

    internal static List<CompetitionEntry> GetList(Competition competition)
    {
        ArgumentNullException.ThrowIfNull(competition);
        return (List<CompetitionEntry>)EntriesField.GetValue(competition)!;
    }

    internal static void Hydrate(Competition competition, IEnumerable<CompetitionEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(entries);

        var list = GetList(competition);
        var ordered = entries as IList<CompetitionEntry> ?? [.. entries];
        if (list.Count == ordered.Count && list.SequenceEqual(ordered))
        {
            return;
        }

        list.Clear();
        list.AddRange(ordered);
    }
}
