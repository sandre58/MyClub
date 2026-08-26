// -----------------------------------------------------------------------
// <copyright file="ConfigureStructureRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for typed V1 structure configuration (not a Domain Format).
/// </summary>
/// <param name="Format">Championship | Groups | Cup | Swiss (case-insensitive).</param>
/// <param name="StageName">Optional primary stage name.</param>
/// <param name="MatchdayCount">Championship matchdays (default 1).</param>
/// <param name="GroupCount">Groups format group count.</param>
/// <param name="ParticipantsPerGroup">Groups format capacity / pot count.</param>
/// <param name="BracketSize">Cup bracket size (power of two, 2–64).</param>
/// <param name="MatchGenerationFormat">SingleRoundRobin | DoubleRoundRobin (Championship/Groups; default Single).</param>
/// <param name="SwissRoundCount">Swiss planned rounds K (≥ 1). Matchdays created by GenerateNextRound.</param>
public sealed record ConfigureStructureRequest(
    string Format,
    string? StageName = null,
    int? MatchdayCount = null,
    int? GroupCount = null,
    int? ParticipantsPerGroup = null,
    int? BracketSize = null,
    string? MatchGenerationFormat = null,
    int? SwissRoundCount = null);
