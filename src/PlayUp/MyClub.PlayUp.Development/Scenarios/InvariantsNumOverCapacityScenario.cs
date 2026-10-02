// -----------------------------------------------------------------------
// <copyright file="InvariantsNumOverCapacityScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Invariants QA — NUM-001 OverCapacityEdge (3 teams on 2 Cup Places).
/// </summary>
public sealed class InvariantsNumOverCapacityScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "invariants-num-overcapacity";

    /// <inheritdoc />
    public string Name => "Invariants — OverCapacityEdge";

    /// <inheritdoc />
    public string Description =>
        "Poules → Barrages 2 Places + Qual Top 4 — OverCapacityEdge sur Barrages (Prepare Gate, ≠ StructureGraphInvalid).";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildInvariantsNumOverCapacityAsync(context, cancellationToken);
}
