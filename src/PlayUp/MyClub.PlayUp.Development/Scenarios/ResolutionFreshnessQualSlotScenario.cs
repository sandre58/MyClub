// -----------------------------------------------------------------------
// <copyright file="ResolutionFreshnessQualSlotScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// League → slot qualification applied; used for Structure resolution freshness E2E.
/// </summary>
public sealed class ResolutionFreshnessQualSlotScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "resolution-freshness-qual-slot";

    /// <inheritdoc />
    public string Name => "RECETTE T3 — Qual slot fraîcheur";

    /// <inheritdoc />
    public string Description =>
        "Finished league, qualification applied to a Terminal slot — occupants start current.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public Recipes.CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildResolutionFreshnessQualSlotAsync(context, cancellationToken);
}
