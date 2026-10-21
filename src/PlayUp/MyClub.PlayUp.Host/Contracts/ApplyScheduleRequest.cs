// -----------------------------------------------------------------------
// <copyright file="ApplyScheduleRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for applying a schedule proposal.
/// </summary>
public sealed record ApplyScheduleRequest(
    IReadOnlyList<ScheduleAssignmentDto> Assignments,
    IReadOnlyList<Guid> TargetMatchIds);
