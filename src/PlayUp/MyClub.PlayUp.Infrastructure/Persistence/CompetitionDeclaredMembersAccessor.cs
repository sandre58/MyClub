// -----------------------------------------------------------------------
// <copyright file="CompetitionDeclaredMembersAccessor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Reads and reorders CompetitionEntry._declaredMembers without exposing a Domain persistence API.
/// </summary>
internal static class CompetitionDeclaredMembersAccessor
{
    private static readonly FieldInfo DeclaredMembersField = typeof(CompetitionEntry).GetField(
        "_declaredMembers",
        BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("CompetitionEntry._declaredMembers backing field was not found.");

    internal static List<DeclaredMember> GetList(CompetitionEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return (List<DeclaredMember>)DeclaredMembersField.GetValue(entry)!;
    }

    internal static void Hydrate(CompetitionEntry entry, IEnumerable<DeclaredMember> members)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(members);

        var list = GetList(entry);
        var ordered = members as IList<DeclaredMember> ?? [.. members];
        if (list.Count == ordered.Count && list.SequenceEqual(ordered))
        {
            return;
        }

        list.Clear();
        list.AddRange(ordered);
    }
}
