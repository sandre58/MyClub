// -----------------------------------------------------------------------
// <copyright file="CompetitionOverviewDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Product overview of a competition for the organizer.
/// </summary>
/// <param name="Id">Competition identity.</param>
/// <param name="Name">Competition display name.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="Entries">Participating entries.</param>
/// <param name="Stages">Referenced stages (name + status).</param>
public sealed record CompetitionOverviewDto(
    Guid Id,
    string Name,
    CompetitionStatus Status,
    IReadOnlyList<CompetitionEntrySummaryDto> Entries,
    IReadOnlyList<CompetitionStageSummaryDto> Stages);

/// <summary>
/// Entry line in a competition overview.
/// </summary>
/// <param name="EntryId">Entry identity.</param>
/// <param name="DisplayName">Display name.</param>
/// <param name="Status">Participation status.</param>
public sealed record CompetitionEntrySummaryDto(Guid EntryId, string DisplayName, EntryStatus Status);

/// <summary>
/// Stage line in a competition overview.
/// </summary>
/// <param name="StageId">Stage identity.</param>
/// <param name="Name">Stage display name.</param>
/// <param name="Status">Stage lifecycle status.</param>
public sealed record CompetitionStageSummaryDto(Guid StageId, string Name, StageStatus Status);
