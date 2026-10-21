// -----------------------------------------------------------------------
// <copyright file="StructureRebuildImpact.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Counts of topology cleared by an explicit structure rebuild (not a Domain concept).
/// </summary>
/// <param name="ClearedMatchdays">Matchdays removed.</param>
/// <param name="ClearedGroups">Groups removed.</param>
/// <param name="ClearedRounds">Rounds removed.</param>
/// <param name="ClearedSlots">Slots removed.</param>
/// <param name="ClearedDirectAssignments">Direct slot assignments cleared.</param>
/// <param name="ClearedCompositionEntries">Root composition entry set cleared.</param>
/// <param name="ClearedDrawRules">Whether DrawRules were cleared.</param>
/// <param name="ClearedSwissSettings">Whether Swiss settings were cleared.</param>
public sealed record StructureRebuildImpact(
    int ClearedMatchdays,
    int ClearedGroups,
    int ClearedRounds,
    int ClearedSlots,
    int ClearedDirectAssignments,
    int ClearedCompositionEntries,
    bool ClearedDrawRules,
    bool ClearedSwissSettings);
