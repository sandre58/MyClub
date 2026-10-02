// -----------------------------------------------------------------------
// <copyright file="CompetitionFlowAnalysis.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Factual flow analysis of a competition (Application diagnostic — not Domain, not persisted).
/// </summary>
/// <remarks>
/// Facts only: no severity, readiness, or Host policy. Prepare and BuildReadiness consume these facts.
/// </remarks>
/// <param name="Feed">WhoFeeds / slot feed diagnostics.</param>
/// <param name="Topology">Graph topology diagnostics (cycles, dangling, …).</param>
/// <param name="Capacity">Volume / capacity diagnostics.</param>
/// <param name="Execution">Execution decidability diagnostics.</param>
/// <param name="Result">Outcome determinability diagnostics (never Ready gates).</param>
public sealed record CompetitionFlowAnalysis(
    IReadOnlyList<CompetitionFlowDiagnostic> Feed,
    IReadOnlyList<CompetitionFlowDiagnostic> Topology,
    IReadOnlyList<CompetitionFlowDiagnostic> Capacity,
    IReadOnlyList<CompetitionFlowDiagnostic> Execution,
    IReadOnlyList<CompetitionFlowDiagnostic> Result)
{
    /// <summary>
    /// Gets an empty analysis.
    /// </summary>
    public static CompetitionFlowAnalysis Empty { get; } = new([], [], [], [], []);
}

/// <summary>
/// One factual diagnostic emitted by <see cref="CompetitionFlowAnalyzer"/>.
/// </summary>
/// <param name="Code">Stable diagnostic code (e.g. SlotFeedMissing).</param>
/// <param name="StageId">Stage subject when applicable.</param>
/// <param name="SlotKey">Cup slot key when applicable.</param>
public sealed record CompetitionFlowDiagnostic(
    string Code,
    Guid? StageId = null,
    string? SlotKey = null);
