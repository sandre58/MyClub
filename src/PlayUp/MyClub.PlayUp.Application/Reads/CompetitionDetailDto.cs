// -----------------------------------------------------------------------
// <copyright file="CompetitionDetailDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Product detail of a competition for the organizer (GET by id — not Overview).
/// </summary>
/// <param name="Id">Competition identity.</param>
/// <param name="Name">Competition display name.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="Entries">Participating entries.</param>
/// <param name="Stages">Referenced stages (name + status).</param>
/// <param name="CompletionMode">How the competition was completed, when set.</param>
/// <param name="ShortName">Optional abbreviated name.</param>
/// <param name="LogoMediaId">Optional Media Guid for the logo.</param>
/// <param name="ScheduledStart">Optional declared start.</param>
/// <param name="ScheduledEnd">Optional declared end.</param>
public sealed record CompetitionDetailDto(
    Guid Id,
    string Name,
    CompetitionStatus Status,
    IReadOnlyList<CompetitionEntrySummaryDto> Entries,
    IReadOnlyList<CompetitionStageSummaryDto> Stages,
    CompletionMode? CompletionMode = null,
    string? ShortName = null,
    Guid? LogoMediaId = null,
    DateTimeOffset? ScheduledStart = null,
    DateTimeOffset? ScheduledEnd = null);

/// <summary>
/// Entry line in a competition detail.
/// </summary>
/// <param name="EntryId">Entry identity.</param>
/// <param name="DisplayName">Display name.</param>
/// <param name="Status">Participation status.</param>
/// <param name="ShortName">Optional abbreviated name.</param>
/// <param name="LogoMediaId">Optional Media Guid for the logo.</param>
/// <param name="PrimaryColor">Optional primary kit color.</param>
/// <param name="SecondaryColor">Optional secondary kit color.</param>
public sealed record CompetitionEntrySummaryDto(
    Guid EntryId,
    string DisplayName,
    EntryStatus Status,
    string? ShortName = null,
    Guid? LogoMediaId = null,
    string? PrimaryColor = null,
    string? SecondaryColor = null);

/// <summary>
/// Stage line in a competition overview.
/// </summary>
/// <param name="StageId">Stage identity.</param>
/// <param name="Name">Stage display name.</param>
/// <param name="Status">Stage lifecycle status.</param>
public sealed record CompetitionStageSummaryDto(Guid StageId, string Name, StageStatus Status);
