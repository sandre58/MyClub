// -----------------------------------------------------------------------
// <copyright file="StructureGraphInvalidScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Draft multi-phase Structure with intentional graph validity issues (dangling Qual population
/// targets) and multi-destination progression — Topology / anomaly QA.
/// </summary>
public sealed class StructureGraphInvalidScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "structure-graph-invalid";

    /// <inheritdoc />
    public string Name => "Structure graph invalid";

    /// <inheritdoc />
    public string Description =>
        "Poules → Barrages → Finale/Bronze in Draft with dangling Qual population targets and multi-dest progression — Structure Topology QA.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildStructureGraphInvalidAsync(context, cancellationToken);
}
