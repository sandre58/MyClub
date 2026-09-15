// -----------------------------------------------------------------------
// <copyright file="FluxFullGraphDraftScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Structure flux QA: Qualif + Progression + Placement on one Draft competition.
/// </summary>
public sealed class FluxFullGraphDraftScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "flux-full-graph-draft";

    /// <inheritdoc />
    public string Name => "Flux — Graphe complet Draft";

    /// <inheritdoc />
    public string Description =>
        "Groups → Demis (Qualif) → Finale/Bronze (Prog + Attribution 1–4) — Draft. Matrice Entrées/Sorties/Attribution.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildFluxFullGraphDraftAsync(context, cancellationToken);
}
