// -----------------------------------------------------------------------
// <copyright file="InvariantsFlowCycleScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Invariants QA — FLOW-001 CycleDetected on Qual population edges.
/// </summary>
public sealed class InvariantsFlowCycleScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "invariants-flow-cycle";

    /// <inheritdoc />
    public string Name => "Invariants — CycleDetected";

    /// <inheritdoc />
    public string Description =>
        "Phase A ↔ Phase B (Qual Population) — CycleDetected (Draft Signal / Prepare Gate → StructureGraphInvalid).";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildInvariantsFlowCycleAsync(context, cancellationToken);
}
