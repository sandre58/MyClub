// -----------------------------------------------------------------------
// <copyright file="WorkspaceSummaryDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Minimal Accueil / workspace landing payload (Slice 1 + Slice 6 completion hints).
/// </summary>
/// <remarks>
/// <see cref="NextActionCode"/> / <see cref="NextActionLabel"/> are Application/Read hints — not Domain fields.
/// <see cref="AttentionCount"/> is a derived Needs Attention count (Slice 5) — not persisted.
/// <see cref="CanCompleteNormally"/> / <see cref="CompletionBlockers"/> are derived CompletionAnalyzer facts — not persisted.
/// </remarks>
/// <param name="Id">Competition identity.</param>
/// <param name="Name">Display name.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="NextActionCode">Stable machine code for the stub next step (e.g. ContinueOrganisation).</param>
/// <param name="NextActionLabel">Organizer-facing next step label.</param>
/// <param name="AttentionCount">Derived Needs Attention count.</param>
/// <param name="CompletionMode">How the competition was completed, when Completed/Archived.</param>
/// <param name="CanCompleteNormally">Whether Complete(Normal) is currently allowed.</param>
/// <param name="CompletionBlockers">Machine reason codes when not sportively complete.</param>
public sealed record WorkspaceSummaryDto(
    Guid Id,
    string Name,
    CompetitionStatus Status,
    string? NextActionCode,
    string? NextActionLabel,
    int AttentionCount,
    CompletionMode? CompletionMode = null,
    bool CanCompleteNormally = false,
    IReadOnlyList<string>? CompletionBlockers = null);
