// -----------------------------------------------------------------------
// <copyright file="FluxQualFormDraftScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Structure flux QA: Qual Place → Championship Forme (ForForm), Draft.
/// </summary>
public sealed class FluxQualFormDraftScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "flux-qual-form-draft";

    /// <inheritdoc />
    public string Name => "Flux — Qual Forme Draft";

    /// <inheritdoc />
    public string Description =>
        "Groups 2×2 (+ 2 directs Champ) → Championnat ForForm (Top1) — Draft. Sac ExpectedFormParticipants (resolved + pending).";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildFluxQualFormDraftAsync(context, cancellationToken);
}
