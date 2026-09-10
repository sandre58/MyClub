// -----------------------------------------------------------------------
// <copyright file="ChampionshipStructureDraftScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Championship structure + matches materialized, stays Draft (healthy Structure edit).
/// </summary>
public sealed class ChampionshipStructureDraftScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "championship-structure-draft";

    /// <inheritdoc />
    public string Name => "Championship structure Draft";

    /// <inheritdoc />
    public string Description => "8-team championship materialized, stays Draft — healthy Structure authoring.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildChampionshipStructureDraftAsync(context, cancellationToken);
}
