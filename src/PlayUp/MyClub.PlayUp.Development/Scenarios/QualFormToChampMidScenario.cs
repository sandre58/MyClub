// -----------------------------------------------------------------------
// <copyright file="QualFormToChampMidScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Groups finished → Qual ForForm applied into Championship (Composition + provenance).
/// </summary>
public sealed class QualFormToChampMidScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "qual-form-to-champ-mid";

    /// <inheritdoc />
    public string Name => "Qual Forme → Champ mid";

    /// <inheritdoc />
    public string Description =>
        "Groups 2×2 finished → Top1 Qual ForForm → Championnat (2 directs + 2 apply) — provenance FormPathResolutions. Champ Draft.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildQualFormToChampMidAsync(context, cancellationToken);
}
