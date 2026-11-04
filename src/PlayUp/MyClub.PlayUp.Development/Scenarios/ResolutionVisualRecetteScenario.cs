// -----------------------------------------------------------------------
// <copyright file="ResolutionVisualRecetteScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Nine RECETTE T3 competitions — resolution freshness badges on Structure (visual recette).
/// </summary>
public sealed class ResolutionVisualRecetteScenario : IScenario
{
    /// <inheritdoc />
    public string Id => ResolutionVisualRecetteNames.ScenarioId;

    /// <inheritdoc />
    public string Name => "RECETTE T3 — Fraîcheur résolution (visuel)";

    /// <inheritdoc />
    public string Description =>
        "One competition per staleness case; final UI state seeded directly (no manual API).";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public Recipes.CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildResolutionVisualRecetteAsync(context, cancellationToken);
}
