// -----------------------------------------------------------------------
// <copyright file="FluxProgPlacementDraftScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Structure flux QA: Progression Winner/Loser + Placement awards 1–4, Draft.
/// </summary>
public sealed class FluxProgPlacementDraftScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "flux-prog-placement-draft";

    /// <inheritdoc />
    public string Name => "Flux — Progression + Attribution Draft";

    /// <inheritdoc />
    public string Description =>
        "Demi 4 (Affectation) → Finale/Bronze : Prog Winner/Loser + Attribution 1–4 — Draft. Sorties Prog + Attribution + Entrées aval.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildFluxProgPlacementDraftAsync(context, cancellationToken);
}
