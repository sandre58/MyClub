// -----------------------------------------------------------------------
// <copyright file="QualFormChampReadyPartialScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Qual ForForm wiring — Championnat Prêt avec composition partielle (directs seulement).
/// </summary>
public sealed class QualFormChampReadyPartialScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "qual-form-champ-ready-partial";

    /// <inheritdoc />
    public string Name => "Qual Forme — Champ Prêt (roster partiel)";

    /// <inheritdoc />
    public string Description =>
        "Groups Draft + Championnat Prêt (2/4 places résolues) — recette empty state Matchs, Materialize/Start bloqués.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildQualFormChampReadyPartialAsync(context, cancellationToken);
}
