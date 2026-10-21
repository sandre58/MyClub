// -----------------------------------------------------------------------
// <copyright file="ScheduleProposalDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Read-friendly schedule generation outcome (not persisted).
/// </summary>
public sealed record ScheduleProposalDto(
    bool IsSuccess,
    bool IsNoSolution,
    bool IsInvalidRequest,
    IReadOnlyList<ScheduleAssignmentDto> Assignments);

/// <summary>One proposed placement.</summary>
public sealed record ScheduleAssignmentDto(Guid MatchId, DateTimeOffset Start, Guid ResourceId);
