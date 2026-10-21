// -----------------------------------------------------------------------
// <copyright file="CompletionAnalysis.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Derived sporting-completeness diagnostic (never persisted).
/// </summary>
/// <param name="IsSportivelyComplete">True when no sporting blockers remain.</param>
/// <param name="CanCompleteNormally">
/// True when sportively complete and Domain allows <c>Complete(Normal)</c>
/// (Running or Suspended).
/// </param>
/// <param name="Reasons">Blockers explaining incompleteness (empty when complete).</param>
public sealed record CompletionAnalysis(
    bool IsSportivelyComplete,
    bool CanCompleteNormally,
    IReadOnlyList<CompletionReasonDto> Reasons);
