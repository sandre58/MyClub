// -----------------------------------------------------------------------
// <copyright file="MatchHubViewDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Match Hub read surface — competition detail + match summaries grouped by stage (one HTTP round-trip).
/// </summary>
/// <param name="Detail">Competition overview (entries + stage summaries).</param>
/// <param name="Stages">Match lists per stage in competition order.</param>
public sealed record MatchHubViewDto(
    CompetitionDetailDto Detail,
    IReadOnlyList<MatchHubStageMatchesDto> Stages);

/// <summary>
/// Match summaries for one competition stage.
/// </summary>
/// <param name="StageId">Stage identity.</param>
/// <param name="Name">Display name.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="Matches">Ordered match summaries (possibly empty).</param>
public sealed record MatchHubStageMatchesDto(
    Guid StageId,
    string Name,
    StageStatus Status,
    IReadOnlyList<MatchSummaryDto> Matches);
