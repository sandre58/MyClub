// -----------------------------------------------------------------------
// <copyright file="RecordedDisciplinaryEventDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Nominative disciplinary fact on a match.
/// </summary>
public sealed record RecordedDisciplinaryEventDto(
    Guid DisciplinaryEventId,
    Guid MemberId,
    string? MemberDisplayName,
    DisciplinaryType Type);
