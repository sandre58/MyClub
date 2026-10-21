// -----------------------------------------------------------------------
// <copyright file="QualificationApplyResponse.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for <c>POST /stages/{stageId}/qualification/apply</c>.
/// </summary>
/// <param name="AppliedCount">Number of population entries applied.</param>
/// <param name="Assignments">Applied population instructions (Guids only — no Domain types).</param>
public sealed record QualificationApplyResponse(
    int AppliedCount,
    IReadOnlyList<QualificationAssignmentDto> Assignments);

/// <summary>
/// One applied qualification population instruction.
/// </summary>
/// <param name="StageId">Destination stage identity.</param>
/// <param name="EntryId">Entry added to the destination population.</param>
public sealed record QualificationAssignmentDto(Guid StageId, Guid EntryId);
