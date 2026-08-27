// -----------------------------------------------------------------------
// <copyright file="CompetitionListItemDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// One row in the organizer Competition List (Slice 1).
/// </summary>
/// <param name="Id">Competition identity.</param>
/// <param name="Name">Display name.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="ShortName">Optional abbreviated display name.</param>
/// <param name="LogoMediaId">Optional Media identity for the competition logo.</param>
public sealed record CompetitionListItemDto(
    Guid Id,
    string Name,
    CompetitionStatus Status,
    string? ShortName = null,
    Guid? LogoMediaId = null);
