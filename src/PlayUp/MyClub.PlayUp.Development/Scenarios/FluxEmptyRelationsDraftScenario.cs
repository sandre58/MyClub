// -----------------------------------------------------------------------
// <copyright file="FluxEmptyRelationsDraftScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Structure flux QA: multi-phase skeleton without Qualif/Prog/Placement edges.
/// </summary>
public sealed class FluxEmptyRelationsDraftScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "flux-empty-relations-draft";

    /// <inheritdoc />
    public string Name => "Flux — Relations vides Draft";

    /// <inheritdoc />
    public string Description =>
        "Groups Affectation + QF slots, aucune arête — Draft. Overflow « Ajouter une sortie » / attribution.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildFluxEmptyRelationsDraftAsync(context, cancellationToken);
}
