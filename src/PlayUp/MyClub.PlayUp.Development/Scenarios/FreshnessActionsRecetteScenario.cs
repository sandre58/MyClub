// -----------------------------------------------------------------------
// <copyright file="FreshnessActionsRecetteScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// One competition for the freshness corrective actions: upstream link, each indeterminate motif, and the attention drawer.
/// </summary>
public sealed class FreshnessActionsRecetteScenario : IScenario
{
    /// <inheritdoc />
    public string Id => FreshnessActionsRecetteNames.ScenarioId;

    /// <inheritdoc />
    public string Name => "RECETTE — Actions de fraîcheur";

    /// <inheritdoc />
    public string Description =>
        "Upstream-stale phase, each indeterminate motif, and À traiter rows, on phases of at least four teams.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public Recipes.CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildFreshnessActionsRecetteAsync(context, cancellationToken);
}
