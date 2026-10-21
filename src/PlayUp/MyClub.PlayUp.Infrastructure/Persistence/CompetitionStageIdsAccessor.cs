// -----------------------------------------------------------------------
// <copyright file="CompetitionStageIdsAccessor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using System.Runtime.CompilerServices;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Reads and hydrates Competition._stageIds without exposing a Domain persistence API.
/// Tracks which Competition instances were hydrated (or added) so the save interceptor
/// never treats an unmapped empty _stageIds list as authoritative.
/// </summary>
internal static class CompetitionStageIdsAccessor
{
    private static readonly FieldInfo StageIdsField = typeof(Competition).GetField(
        "_stageIds",
        BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Competition._stageIds backing field was not found.");

    private static readonly ConditionalWeakTable<Competition, object> Hydrated = [];

    internal static List<StageId> GetList(Competition competition)
    {
        ArgumentNullException.ThrowIfNull(competition);
        return (List<StageId>)StageIdsField.GetValue(competition)!;
    }

    internal static void Hydrate(Competition competition, IEnumerable<StageId> stageIds)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stageIds);

        var list = GetList(competition);
        list.Clear();
        list.AddRange(stageIds);
        MarkHydrated(competition);
    }

    internal static void MarkHydrated(Competition competition)
    {
        ArgumentNullException.ThrowIfNull(competition);
        Hydrated.AddOrUpdate(competition, Sentinel);
    }

    internal static bool IsHydrated(Competition competition)
    {
        ArgumentNullException.ThrowIfNull(competition);
        return Hydrated.TryGetValue(competition, out _);
    }

    private static readonly object Sentinel = new();
}
