// -----------------------------------------------------------------------
// <copyright file="CupSfRunningScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Multi-stage Cup Running with SF half-played (healthy mid-bracket ops).
/// </summary>
public sealed class CupSfRunningScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "cup-sf-running";

    /// <inheritdoc />
    public string Name => "Cup SF running";

    /// <inheritdoc />
    public string Description =>
        "QF Prog Auto Place → SF slots · SF materialized ~50% played — multi-phase Running. Contrast: cup-qf-sf stops before from-slots.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildCupSfRunningAsync(context, cancellationToken);
}
