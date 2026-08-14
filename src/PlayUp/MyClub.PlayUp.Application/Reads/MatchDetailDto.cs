// -----------------------------------------------------------------------
// <copyright file="MatchDetailDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Match detail for Start/Finish context (no Winner).
/// </summary>
/// <param name="MatchId">Match identity.</param>
/// <param name="CompetitionId">Owning competition.</param>
/// <param name="StageId">Owning stage.</param>
/// <param name="Status">Match lifecycle status.</param>
/// <param name="Home">Home side.</param>
/// <param name="Away">Away side.</param>
/// <param name="Result">Full result when finished; otherwise <see langword="null"/>.</param>
/// <param name="FixtureId">Owning fixture when attached.</param>
/// <param name="LegIndex">Leg index when attached.</param>
public sealed record MatchDetailDto(
    Guid MatchId,
    Guid CompetitionId,
    Guid StageId,
    MatchStatus Status,
    EntrySideDto Home,
    EntrySideDto Away,
    MatchResultDto? Result,
    Guid? FixtureId,
    int? LegIndex);
