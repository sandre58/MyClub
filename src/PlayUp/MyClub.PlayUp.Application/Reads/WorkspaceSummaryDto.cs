// -----------------------------------------------------------------------
// <copyright file="WorkspaceSummaryDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Minimal Accueil / workspace landing payload (Slice 1).
/// </summary>
/// <remarks>
/// <see cref="NextActionCode"/> / <see cref="NextActionLabel"/> are Application/Read hints — not Domain fields.
/// <see cref="AttentionCount"/> is always 0 in Slice 1 (Needs Attention arrives in a later slice).
/// </remarks>
/// <param name="Id">Competition identity.</param>
/// <param name="Name">Display name.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="NextActionCode">Stable machine code for the stub next step (e.g. ContinueOrganisation).</param>
/// <param name="NextActionLabel">Organizer-facing next step label.</param>
/// <param name="AttentionCount">Needs Attention count (0 in Slice 1).</param>
public sealed record WorkspaceSummaryDto(
    Guid Id,
    string Name,
    CompetitionStatus Status,
    string? NextActionCode,
    string? NextActionLabel,
    int AttentionCount);
