// -----------------------------------------------------------------------
// <copyright file="AddCompetitionStageRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for atomic stage birth (identity + skeleton).
/// </summary>
/// <param name="Format">Championship | Groups | Cup | Swiss (case-insensitive).</param>
/// <param name="Name">Stage display name.</param>
/// <param name="MatchdayCount">Championship matchdays (≥ 1).</param>
/// <param name="GroupCount">Groups format group count (≥ 2).</param>
/// <param name="ParticipantsPerGroup">Groups places per group (≥ 2) — capacity, not population.</param>
/// <param name="BracketSize">Cup bracket size (power of two, 2–64).</param>
/// <param name="MatchGenerationFormat">SingleRoundRobin | DoubleRoundRobin (Championship/Groups).</param>
/// <param name="SwissRoundCount">Swiss planned rounds K (≥ 1).</param>
public sealed record AddCompetitionStageRequest(
    string Format,
    string Name,
    int? MatchdayCount = null,
    int? GroupCount = null,
    int? ParticipantsPerGroup = null,
    int? BracketSize = null,
    string? MatchGenerationFormat = null,
    int? SwissRoundCount = null);

/// <summary>
/// HTTP response after atomic stage birth.
/// </summary>
/// <param name="StageId">New stage identity.</param>
/// <param name="Name">Normalized stage name.</param>
/// <param name="Structure">Updated structure read model.</param>
public sealed record AddCompetitionStageResponse(
    Guid StageId,
    string Name,
    StructureViewDto Structure);
