// -----------------------------------------------------------------------
// <copyright file="MatchDeclaredParticipationsAccessor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Reads and reorders Match._declaredParticipations without exposing a Domain persistence API.
/// </summary>
internal static class MatchDeclaredParticipationsAccessor
{
    private static readonly FieldInfo DeclaredParticipationsField = typeof(Match).GetField(
        "_declaredParticipations",
        BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Match._declaredParticipations backing field was not found.");

    internal static List<DeclaredParticipation> GetList(Match match)
    {
        ArgumentNullException.ThrowIfNull(match);
        return (List<DeclaredParticipation>)DeclaredParticipationsField.GetValue(match)!;
    }

    internal static void Hydrate(Match match, IEnumerable<DeclaredParticipation> participations)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(participations);

        var list = GetList(match);
        var ordered = participations as IList<DeclaredParticipation> ?? [.. participations];
        if (list.Count == ordered.Count && list.SequenceEqual(ordered))
        {
            return;
        }

        list.Clear();
        list.AddRange(ordered);
    }
}
