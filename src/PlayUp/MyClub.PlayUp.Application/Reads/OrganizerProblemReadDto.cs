// -----------------------------------------------------------------------
// <copyright file="OrganizerProblemReadDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Application-side organizer problem semantics on read models (pilot — not ProblemDetails).
/// </summary>
/// <param name="CanonCode">Most precise stable fact code.</param>
/// <param name="SemanticLevel"><see cref="OrganizerProblemSemanticLevels"/> value.</param>
/// <param name="BlockedActions">Organizer action codes blocked by this problem (empty when none).</param>
/// <param name="TransportCode">Wire code on this DTO surface when it differs from canon (alias / aggregate).</param>
/// <param name="SuggestedActionCode">Optional remediation action (Attention), not a block.</param>
public sealed record OrganizerProblemReadDto(
    string CanonCode,
    string SemanticLevel,
    IReadOnlyList<string> BlockedActions,
    string? TransportCode = null,
    string? SuggestedActionCode = null);
