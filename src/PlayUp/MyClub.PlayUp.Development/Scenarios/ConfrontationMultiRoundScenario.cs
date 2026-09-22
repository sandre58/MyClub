// -----------------------------------------------------------------------
// <copyright file="ConfrontationMultiRoundScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Structure Confrontation QA: one phase, three rounds with distinct TieFormats.
/// </summary>
public sealed class ConfrontationMultiRoundScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "confrontation-multi-round";

    /// <inheritdoc />
    public string Name => "Démo Confrontation multi-tours";

    /// <inheritdoc />
    public string Description =>
        "Cup 8 · Tableau QF/SF/Finale (fixtures + TieFormats) · Phase aval vide · Draft — Sorties Progression authorables.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildConfrontationMultiRoundDemoAsync(context, cancellationToken);
}
