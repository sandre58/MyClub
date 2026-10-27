// -----------------------------------------------------------------------
// <copyright file="CtrlO3MatRecetteScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Suspended-phase production UI gate (see docs/design/structure-lifecycle/ctrl-o3-mat-devrunner-recette.md).
/// </summary>
public sealed class CtrlO3MatRecetteScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "ctrl-o3-mat-recette";

    /// <inheritdoc />
    public string Name => "CTRL-O3-MAT — Suspendu / production";

    /// <inheritdoc />
    public string Description =>
        "S1–S4 competitions for materialize/dematerialize assertions when phase or competition is Suspended.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildCtrlO3MatRecetteAsync(context, cancellationToken);
}
