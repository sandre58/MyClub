// -----------------------------------------------------------------------
// <copyright file="FluxQualifDraftScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Structure flux QA: Affectation + outbound Qualification configured, Draft (no play).
/// </summary>
public sealed class FluxQualifDraftScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "flux-qualif-draft";

    /// <inheritdoc />
    public string Name => "Flux — Qualification Draft";

    /// <inheritdoc />
    public string Description =>
        "Groups 2×4 Affectation + Qualif Top1/Top2 → QF population — Draft. Sorties Qualif + Entrées aval (WhoFeeds / jump source).";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildFluxQualifDraftAsync(context, cancellationToken);
}
