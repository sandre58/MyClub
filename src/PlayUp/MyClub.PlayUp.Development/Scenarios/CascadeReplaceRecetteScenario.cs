// -----------------------------------------------------------------------
// <copyright file="CascadeReplaceRecetteScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// RECETTE T4 competitions left in the stale state a cascade confirmation would repair.
/// </summary>
public sealed class CascadeReplaceRecetteScenario : IScenario
{
    /// <inheritdoc />
    public string Id => CascadeReplaceRecetteNames.ScenarioId;

    /// <inheritdoc />
    public string Name => "RECETTE T4 — Remplacement en cascade";

    /// <inheritdoc />
    public string Description =>
        "Stale qualification chains: an unplayed downstream match, a played one, a three-level chain, a completed phase, and a direct assignment.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public Recipes.CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildCascadeReplaceRecetteAsync(context, cancellationToken);
}
