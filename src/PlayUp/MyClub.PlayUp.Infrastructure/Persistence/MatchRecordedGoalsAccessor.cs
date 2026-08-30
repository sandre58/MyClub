// -----------------------------------------------------------------------
// <copyright file="MatchRecordedGoalsAccessor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Reads and reorders Match._recordedGoals without exposing a Domain persistence API.
/// </summary>
internal static class MatchRecordedGoalsAccessor
{
    private static readonly FieldInfo RecordedGoalsField = typeof(Match).GetField(
        "_recordedGoals",
        BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Match._recordedGoals backing field was not found.");

    internal static List<RecordedGoal> GetList(Match match)
    {
        ArgumentNullException.ThrowIfNull(match);
        return (List<RecordedGoal>)RecordedGoalsField.GetValue(match)!;
    }

    internal static void Hydrate(Match match, IEnumerable<RecordedGoal> goals)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(goals);

        var list = GetList(match);
        var ordered = goals as IList<RecordedGoal> ?? [.. goals];
        if (list.Count == ordered.Count && list.SequenceEqual(ordered))
        {
            return;
        }

        list.Clear();
        list.AddRange(ordered);
    }
}
