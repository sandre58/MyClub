// -----------------------------------------------------------------------
// <copyright file="MatchRecordedSubstitutionsAccessor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Reads and reorders Match._recordedSubstitutions without exposing a Domain persistence API.
/// </summary>
internal static class MatchRecordedSubstitutionsAccessor
{
    private static readonly FieldInfo RecordedSubstitutionsField = typeof(Match).GetField(
        "_recordedSubstitutions",
        BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Match._recordedSubstitutions backing field was not found.");

    internal static List<RecordedSubstitution> GetList(Match match)
    {
        ArgumentNullException.ThrowIfNull(match);
        return (List<RecordedSubstitution>)RecordedSubstitutionsField.GetValue(match)!;
    }

    internal static void Hydrate(Match match, IEnumerable<RecordedSubstitution> substitutions)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(substitutions);

        var list = GetList(match);
        var ordered = substitutions as IList<RecordedSubstitution> ?? [.. substitutions];
        if (list.Count == ordered.Count && list.SequenceEqual(ordered))
        {
            return;
        }

        list.Clear();
        list.AddRange(ordered);
    }
}
