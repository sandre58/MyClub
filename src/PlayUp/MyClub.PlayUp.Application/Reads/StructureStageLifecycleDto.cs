// -----------------------------------------------------------------------
// <copyright file="StructureStageLifecycleDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Per-phase lifecycle projection for Structure N2 (Prepare / Start / Complete band).
/// </summary>
/// <param name="Transition">Primary lifecycle command for the current stage status.</param>
/// <param name="MatchProgress">Match closure stats when the phase is in play; otherwise <see langword="null"/>.</param>
/// <param name="RevertTransition">Optional secondary command (Ready → Draft, Running → Suspend, Suspended → Resume).</param>
/// <param name="CancelStartTransition">Running → Ready when no match has started; omitted when unavailable.</param>
public sealed record StructureStageLifecycleDto(
    StructureStageLifecycleTransitionDto Transition,
    StructureStageLifecycleMatchProgressDto? MatchProgress,
    StructureStageLifecycleTransitionDto? RevertTransition = null,
    StructureStageLifecycleTransitionDto? CancelStartTransition = null);

/// <summary>
/// Projected lifecycle transition (Overview action codes + CompleteStage).
/// </summary>
/// <param name="Code"><c>PrepareStage</c>, <c>StartStage</c>, <c>CompleteStage</c>, revert codes, or <see langword="null"/>.</param>
/// <param name="Enabled">When <see langword="true"/>, Host accepts the command for this phase.</param>
/// <param name="DisabledReasonCode">Stable machine code when <paramref name="Code"/> is set and <paramref name="Enabled"/> is false.</param>
/// <param name="Remediation">Optional deep-link for the first corrective step.</param>
/// <param name="Problem">Optional Step 2 semantics (canon, level, blocked actions); pilot read models.</param>
public sealed record StructureStageLifecycleTransitionDto(
    string? Code,
    bool Enabled,
    string? DisabledReasonCode = null,
    StructureStageLifecycleRemediationDto? Remediation = null,
    OrganizerProblemReadDto? Problem = null);

/// <summary>
/// SPA deep-link remediation (Structure hub query contract).
/// </summary>
/// <param name="Href">Relative URL (e.g. <c>/competitions/{id}/structure?stage=…&amp;section=…</c>).</param>
/// <param name="LabelKey">i18n key segment under <c>structure.lifecycle.remediation</c>.</param>
public sealed record StructureStageLifecycleRemediationDto(string Href, string LabelKey);

/// <summary>
/// Match closure stats for Running / Suspended / Completed bands.
/// </summary>
/// <param name="Total">Matches attached to the stage.</param>
/// <param name="Finished">Finished matches.</param>
/// <param name="Cancelled">Cancelled matches.</param>
/// <param name="Open">Matches neither finished nor cancelled.</param>
/// <param name="CanComplete">True when stage completion would succeed (Gate A + C).</param>
public sealed record StructureStageLifecycleMatchProgressDto(
    int Total,
    int Finished,
    int Cancelled,
    int Open,
    bool CanComplete);
