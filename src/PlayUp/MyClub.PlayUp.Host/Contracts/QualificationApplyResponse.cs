// -----------------------------------------------------------------------
// <copyright file="QualificationApplyResponse.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for <c>POST /stages/{stageId}/qualification/apply</c>.
/// </summary>
/// <param name="AppliedCount">Number of slot assignments applied.</param>
/// <param name="Assignments">Applied assignments (Guids only — no Domain types).</param>
public sealed record QualificationApplyResponse(
    int AppliedCount,
    IReadOnlyList<QualificationAssignmentDto> Assignments);

/// <summary>
/// One applied qualification slot assignment.
/// </summary>
/// <param name="StageId">Destination stage identity.</param>
/// <param name="SlotKey">Destination slot key.</param>
/// <param name="EntryId">Entry placed in the slot.</param>
public sealed record QualificationAssignmentDto(Guid StageId, string SlotKey, Guid EntryId);
