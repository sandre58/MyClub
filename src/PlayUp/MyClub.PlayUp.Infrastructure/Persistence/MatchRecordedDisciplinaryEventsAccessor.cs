// -----------------------------------------------------------------------
// <copyright file="MatchRecordedDisciplinaryEventsAccessor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Reads and reorders Match._recordedDisciplinaryEvents without exposing a Domain persistence API.
/// </summary>
internal static class MatchRecordedDisciplinaryEventsAccessor
{
    private static readonly FieldInfo RecordedDisciplinaryEventsField = typeof(Match).GetField(
        "_recordedDisciplinaryEvents",
        BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Match._recordedDisciplinaryEvents backing field was not found.");

    internal static List<RecordedDisciplinaryEvent> GetList(Match match)
    {
        ArgumentNullException.ThrowIfNull(match);
        return (List<RecordedDisciplinaryEvent>)RecordedDisciplinaryEventsField.GetValue(match)!;
    }

    internal static void Hydrate(Match match, IEnumerable<RecordedDisciplinaryEvent> events)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(events);

        var list = GetList(match);
        var ordered = events as IList<RecordedDisciplinaryEvent> ?? [.. events];
        if (list.Count == ordered.Count && list.SequenceEqual(ordered))
        {
            return;
        }

        list.Clear();
        list.AddRange(ordered);
    }
}
