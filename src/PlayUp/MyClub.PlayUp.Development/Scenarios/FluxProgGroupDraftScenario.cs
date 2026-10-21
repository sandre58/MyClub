// -----------------------------------------------------------------------
// <copyright file="FluxProgGroupDraftScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Structure flux QA: Prog Place → Groups poule (ForGroup), Draft.
/// </summary>
public sealed class FluxProgGroupDraftScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "flux-prog-group-draft";

    /// <inheritdoc />
    public string Name => "Flux — Prog Groupe Draft";

    /// <inheritdoc />
    public string Description =>
        "Demi 4 (Affectation) → Winner Prog ForGroup → 2 poules aval — Draft. WhoFeeds grain Groupe.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildFluxProgGroupDraftAsync(context, cancellationToken);
}
