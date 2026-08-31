// -----------------------------------------------------------------------
// <copyright file="RecordedSubstitutionDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Ordered substitution fact on a match.
/// </summary>
public sealed record RecordedSubstitutionDto(
    Guid SubstitutionId,
    Side Side,
    Guid OutMemberId,
    string? OutDisplayName,
    Guid InMemberId,
    string? InDisplayName);
