// -----------------------------------------------------------------------
// <copyright file="DrawSummaryDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Lightweight draw status for Host responses.
/// </summary>
public sealed record DrawSummaryDto(
    Guid DrawId,
    Guid StageId,
    DrawResolutionKind Kind,
    DrawStatus Status,
    DrawResolutionState ResolutionState,
    bool IsNoSolution);

/// <summary>
/// Outcome of generate draw for Host.
/// </summary>
public sealed record DrawGenerationDto(
    Guid DrawId,
    bool IsResolved,
    bool IsNoSolution,
    DrawStatus Status,
    DrawResolutionState ResolutionState);
