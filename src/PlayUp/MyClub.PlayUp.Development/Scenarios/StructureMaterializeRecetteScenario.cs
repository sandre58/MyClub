// -----------------------------------------------------------------------
// <copyright file="StructureMaterializeRecetteScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Structure → Materialize UI recette: one competition per row (Groups / Cup / Running).
/// </summary>
public sealed class StructureMaterializeRecetteScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "structure-materialize-recette";

    /// <inheritdoc />
    public string Name => "Structure — materialize recette";

    /// <inheritdoc />
    public string Description =>
        "RECETTE G1–G3, C1–C3, R1–R3 — deterministic materialize states for Structure production UI QA.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildStructureMaterializeRecetteAsync(context, cancellationToken);
}
