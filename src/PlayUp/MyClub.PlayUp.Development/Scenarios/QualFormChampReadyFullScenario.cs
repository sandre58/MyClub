// -----------------------------------------------------------------------
// <copyright file="QualFormChampReadyFullScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Qual ForForm appliquée — Championnat Prêt, roster complet, sans matchs matérialisés.
/// </summary>
public sealed class QualFormChampReadyFullScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "qual-form-champ-ready-full";

    /// <inheritdoc />
    public string Name => "Qual Forme — Champ Prêt (roster complet)";

    /// <inheritdoc />
    public string Description =>
        "Poules terminées, 4 finalistes connus, compétition Prêt — Materialize actif, Start bloqué jusqu'à production.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildQualFormChampReadyFullAsync(context, cancellationToken);
}
