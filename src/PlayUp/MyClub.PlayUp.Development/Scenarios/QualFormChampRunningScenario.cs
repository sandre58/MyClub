// -----------------------------------------------------------------------
// <copyright file="QualFormChampRunningScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Qual ForForm — Championnat en cours avec matchs matérialisés.
/// </summary>
public sealed class QualFormChampRunningScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "qual-form-champ-running";

    /// <inheritdoc />
    public string Name => "Qual Forme — Championnat en cours";

    /// <inheritdoc />
    public string Description =>
        "Poules terminées, championnat matérialisé et démarré — recette exploitation / calendrier.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildQualFormChampRunningAsync(context, cancellationToken);
}
