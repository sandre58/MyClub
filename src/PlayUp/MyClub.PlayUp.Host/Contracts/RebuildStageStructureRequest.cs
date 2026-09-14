// -----------------------------------------------------------------------
// <copyright file="RebuildStageStructureRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for same-kind skeleton rebuild of a stage.
/// </summary>
/// <param name="Format">Must match the stage's current format kind.</param>
/// <param name="StageName">Optional rename; null keeps current name via mapper fallback.</param>
/// <param name="MatchdayCount">Championship matchdays.</param>
/// <param name="GroupCount">Groups group count.</param>
/// <param name="ParticipantsPerGroup">Groups places per group.</param>
/// <param name="BracketSize">Cup bracket size.</param>
/// <param name="MatchGenerationFormat">Championship/Groups RR mode.</param>
/// <param name="SwissRoundCount">Swiss K.</param>
public sealed record RebuildStageStructureRequest(
    string Format,
    string? StageName = null,
    int? MatchdayCount = null,
    int? GroupCount = null,
    int? ParticipantsPerGroup = null,
    int? BracketSize = null,
    string? MatchGenerationFormat = null,
    int? SwissRoundCount = null);

/// <summary>
/// HTTP response after skeleton rebuild.
/// </summary>
public sealed record RebuildStageStructureResponse(
    StructureRebuildImpactDto Impact,
    StructureViewDto Structure);
