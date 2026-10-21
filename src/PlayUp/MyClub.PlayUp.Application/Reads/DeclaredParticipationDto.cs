// -----------------------------------------------------------------------
// <copyright file="DeclaredParticipationDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Declared composition line on a match sheet.
/// </summary>
public sealed record DeclaredParticipationDto(
    Guid MemberId,
    string? DisplayName,
    Side Side,
    CompositionStatus CompositionStatus,
    int? JerseyNumber);
