// -----------------------------------------------------------------------
// <copyright file="NeedsAttentionItemDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// One Needs Attention item (Application projection — not a Domain entity).
/// </summary>
/// <remarks>
/// Organizer copy lives in the SPA i18n layer keyed by <see cref="Source"/> (+ <see cref="Params"/>).
/// </remarks>
/// <param name="Source">Stable source kind (e.g. DrawNoSolution, ProgressionPending).</param>
/// <param name="Severity">Diagnostic severity.</param>
/// <param name="TargetType">Optional target kind (Stage, Draw, Fixture, Slot…).</param>
/// <param name="TargetId">Optional target identity as string.</param>
/// <param name="Params">Optional structured params for SPA templates (e.g. activeCount, minimumTeams).</param>
public sealed record NeedsAttentionItemDto(
    string Source,
    string Severity,
    string? TargetType,
    string? TargetId,
    IReadOnlyDictionary<string, string>? Params = null);
