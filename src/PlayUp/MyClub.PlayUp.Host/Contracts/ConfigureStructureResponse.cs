// -----------------------------------------------------------------------
// <copyright file="ConfigureStructureResponse.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP response after bootstrap or explicit structure rebuild.
/// </summary>
/// <param name="StageCreated">True when the primary stage was created.</param>
/// <param name="RebuildImpact">Cleared topology when an existing skeleton was rebuilt.</param>
/// <param name="Structure">Refreshed Structure hub view.</param>
public sealed record ConfigureStructureResponse(
    bool StageCreated,
    StructureRebuildImpactDto? RebuildImpact,
    StructureViewDto Structure);

/// <summary>
/// Cleared topology counts for an explicit rebuild.
/// </summary>
public sealed record StructureRebuildImpactDto(
    int ClearedMatchdays,
    int ClearedGroups,
    int ClearedRounds,
    int ClearedSlots,
    int ClearedDirectAssignments,
    int ClearedCompositionEntries,
    bool ClearedDrawRules,
    bool ClearedSwissSettings);
