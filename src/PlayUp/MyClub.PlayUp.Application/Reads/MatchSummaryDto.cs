// -----------------------------------------------------------------------
// <copyright file="MatchSummaryDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Match list line for a stage (no Winner).
/// </summary>
/// <param name="MatchId">Match identity.</param>
/// <param name="StageId">Owning stage.</param>
/// <param name="Status">Match lifecycle status.</param>
/// <param name="Home">Home side.</param>
/// <param name="Away">Away side.</param>
/// <param name="Score">Play score when finished; otherwise <see langword="null"/>.</param>
/// <param name="FixtureId">Owning fixture when attached.</param>
/// <param name="RoundId">Owning round when the fixture is in a round.</param>
/// <param name="ScheduledAt">Optional calendar start from Stage placement.</param>
/// <param name="ResourceId">Optional scheduling resource from Stage placement.</param>
public sealed record MatchSummaryDto(
    Guid MatchId,
    Guid StageId,
    MatchStatus Status,
    EntrySideDto Home,
    EntrySideDto Away,
    MatchScoreDto? Score,
    Guid? FixtureId,
    Guid? RoundId,
    DateTimeOffset? ScheduledAt = null,
    Guid? ResourceId = null);
